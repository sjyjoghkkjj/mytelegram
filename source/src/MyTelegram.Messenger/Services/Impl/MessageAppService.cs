namespace MyTelegram.Messenger.Services.Impl;

public class MessageAppService(
    IQueryProcessor queryProcessor,
    ICommandBus commandBus,
    IObjectMapper objectMapper,
    IPeerHelper peerHelper,
    IBlockCacheAppService blockCacheAppService,
    IPhotoAppService photoAppService,
    IChannelAppService channelAppService,
    IUserAppService userAppService,
    IPrivacyAppService privacyAppService,
    IOffsetHelper offsetHelper,
    IIdGenerator idGenerator)
    : BaseAppService, IMessageAppService, ITransientDependency
{
    public async Task<GetMessageOutput> GetChannelDifferenceAsync(GetDifferenceInput input)
    {
        return await GetMessagesInternalAsync(new GetMessagesQuery(input.OwnerPeerId,
            MessageType.Unknown,
            null,
            input.MessageIds,
            0,
            input.Limit,
            null,
            null,
            input.SelfUserId,
            input.Pts), input.Users, input.Chats);
    }

    public Task<GetMessageOutput> GetDifferenceAsync(GetDifferenceInput input)
    {
        return GetMessagesInternalAsync(new GetMessagesQuery(input.OwnerPeerId,
            MessageType.Unknown,
            null,
            null,
            0,
            input.Limit,
            null,
            null,
            input.SelfUserId,
            input.Pts), input.Users, input.Chats);
    }

    public Task<GetMessageOutput> GetHistoryAsync(GetHistoryInput input)
    {
        return GetMessagesCoreAsync(input);
    }

    public Task<GetMessageOutput> GetMessagesAsync(GetMessagesInput input)
    {
        return GetMessagesCoreAsync(input);
    }

    public Task<GetMessageOutput> GetRepliesAsync(GetRepliesInput input)
    {
        return GetMessagesCoreAsync(input);
    }

    public Task<GetMessageOutput> SearchAsync(SearchInput input)
    {
        return GetMessagesCoreAsync(input);
    }

    public Task<GetMessageOutput> SearchGlobalAsync(SearchGlobalInput input)
    {
        return GetMessagesCoreAsync(input);
    }

    private async Task CheckBlockedAsync(SendMessageInput input)
    {
        if (input.ToPeer.PeerType == PeerType.User)
        {
            if (await blockCacheAppService.IsBlockedAsync(input.ToPeer.PeerId, input.SenderUserId))
            {
                RpcErrors.RpcErrors400.UserIsBlocked.ThrowRpcError();
            }

            if (await blockCacheAppService.IsBlockedAsync(input.SenderUserId, input.ToPeer.PeerId))
            {
                RpcErrors.RpcErrors400.YouBlockedUser.ThrowRpcError();
            }
        }
    }

    public async Task CheckSendAsAsync(long requestUserId, Peer toPeer, Peer? sendAs)
    {
        if (sendAs != null)
        {
            if (toPeer.PeerType != PeerType.Channel)
            {
                RpcErrors.RpcErrors400.SendAsPeerInvalid.ThrowRpcError();
            }

            switch (sendAs.PeerType)
            {
                case PeerType.User:
                case PeerType.Self:
                    if (sendAs.PeerId != requestUserId)
                    {
                        RpcErrors.RpcErrors400.SendAsPeerInvalid.ThrowRpcError();
                    }
                    break;

                case PeerType.Channel:

                    var sendAsPeerId =
                        await queryProcessor.ProcessAsync(new GetSendAsPeerIdQuery(requestUserId, sendAs.PeerId));
                    if (sendAsPeerId == null)
                    {
                        RpcErrors.RpcErrors400.SendAsPeerInvalid.ThrowRpcError();
                    }

                    break;
            }
        }
    }

    private Task CheckSendAsAsync(SendMessageInput input)
    {
        return CheckSendAsAsync(input.RequestInfo.UserId, input.ToPeer, input.SendAs);
    }

    private async Task<IChannelReadModel?> CheckChannelBannedRightsAsync(SendMessageInput input)
    {
        if (input.ToPeer.PeerType != PeerType.Channel)
        {
            return null;
        }

        var channelReadModel = await channelAppService.GetAsync(input.ToPeer.PeerId);
        if (channelReadModel!.Broadcast)
        {
            var admin = channelReadModel.AdminList.FirstOrDefault(p => p.UserId == input.SenderUserId);
            if (admin == null || !admin.AdminRights.PostMessages)
            {
                RpcErrors.RpcErrors403.ChatWriteForbidden.ThrowRpcError();
            }
        }

        var bannedDefaultRights = channelReadModel.DefaultBannedRights ?? ChatBannedRights.CreateDefaultBannedRights();
        if (bannedDefaultRights.SendMessages)
        {
            RpcErrors.RpcErrors403.ChatWriteForbidden.ThrowRpcError();
        }

        var channelMemberReadModel =
            await queryProcessor.ProcessAsync(new GetChannelMemberByUserIdQuery(channelReadModel.ChannelId,
                input.SenderUserId));

        if (channelMemberReadModel == null)
        {
            RpcErrors.RpcErrors403.ChatGuestSendForbidden.ThrowRpcError();
        }

        if (channelMemberReadModel!.BannedRights != 0)
        {
            var memberBannedRights =
                ChatBannedRights.FromValue(channelMemberReadModel.BannedRights, channelMemberReadModel.UntilDate);
            if (!string.IsNullOrEmpty(input.Message))
            {
                if (memberBannedRights.SendMessages)
                {
                    RpcErrors.RpcErrors400.UserBannedInChannel.ThrowRpcError();
                }
            }
            if (input.Media != null)
            {
                if (memberBannedRights.SendMedia)
                {
                    RpcErrors.RpcErrors400.UserBannedInChannel.ThrowRpcError();
                }
            }
        }

        return channelReadModel;
    }

    private async Task<(List<long> mentionedUserIds, TVector<IMessageEntity>? entities)> GetMessageEntitiesAsync(SendMessageInput input)
    {
        var mentionsAndUserNames = GetMentions(input.Message);
        var mentions = mentionsAndUserNames.mentions;
        var mentionedUserNames = mentionsAndUserNames.userNameList;
        var entities = input.Entities == null ? null : new TVector<IMessageEntity>(input.Entities);
        var mentionedUserIds = new List<long>();

        if (input.Entities?.Count > 0)
        {
            foreach (var messageEntity in input.Entities)
            {
                switch (messageEntity)
                {
                    case TInputMessageEntityMentionName inputMessageEntityMentionName:
                        var userPeer = peerHelper.GetPeer(inputMessageEntityMentionName.UserId);
                        mentionedUserIds.Add(userPeer.PeerId);
                        break;
                    case TMessageEntityMention messageEntityMention:
                        mentionedUserNames.Add(input.Message.Substring(messageEntityMention.Offset + 1,
                            messageEntityMention.Length - 1));
                        break;
                    case TMessageEntityMentionName messageEntityMentionName:
                        mentionedUserIds.Add(messageEntityMentionName.UserId);
                        break;
                }
            }
        }

        if (mentionedUserNames.Count > 0)
        {
            var mentionedUsers =
                await queryProcessor.ProcessAsync(new GetUserNameListByNamesQuery(mentionedUserNames, PeerType.User));
            mentionedUserIds.AddRange(mentionedUsers.Select(p => p.PeerId).Distinct().ToList());

            entities ??= new TVector<IMessageEntity>();
            foreach (var messageEntityMention in mentions)
            {
                entities.Add(messageEntityMention);
            }
        }

        return (mentionedUserIds, entities);
    }

    private async Task<List<long>?> GetChatMembersAsync(SendMessageInput input)
    {
        if (input.ToPeer.PeerType != PeerType.Chat)
        {
            return null;
        }

        var chatReadModel = await queryProcessor.ProcessAsync(new GetChatByChatIdQuery(input.ToPeer.PeerId));
        if (chatReadModel == null)
        {
            RpcErrors.RpcErrors400.ChatIdInvalid.ThrowRpcError();
        }

        return chatReadModel!.ChatMembers.Select(p => p.UserId).ToList();
    }

    public async Task SendMessageAsync(List<SendMessageInput> inputs)
    {
        if (inputs.Count == 0)
        {
            throw new ArgumentException();
        }

        List<SendMessageItem> sendMessageItems = [];
        var firstInput = inputs.First();
        var requestInfo = firstInput.RequestInfo;
        foreach (var input in inputs)
        {
            var item = await CreateSendMessageItemAsync(input);
            sendMessageItems.Add(item);
        }

        var command = new StartSendMessageCommand(TempId.New, requestInfo,
            sendMessageItems,
            firstInput.ClearDraft,
            firstInput.IsSendGroupedMessage,
            firstInput.IsSendQuickReplyMessage);

        await commandBus.PublishAsync(command);
    }

    private async Task<SendMessageItem> CreateSendMessageItemAsync(SendMessageInput input)
    {
        //await CheckAccessHashAsync(input);
        await CheckSendAsAsync(input);
        await CheckBlockedAsync(input);
        var channelReadModel = await CheckChannelBannedRightsAsync(input);
        var chatMembers = await GetChatMembersAsync(input);

        var item = await GetMessageEntitiesAsync(input);
        var ownerPeerId = input.ToPeer.PeerType == PeerType.Channel ? input.ToPeer.PeerId : input.SenderUserId;
        var replyToMsgId = input.InputReplyTo.ToReplyToMsgId();

        // Reply to group: ToPeerId=input.ToPeerId,SenderUserId=input.UserId
        // Reply to user:  ToPeerId=Input.UserId,OwnerPeerId=input.ToPeerId,MessageId=replyToMsgId

        var replyToMsgItems =
            await queryProcessor.ProcessAsync(new GetReplyToMsgIdListQuery(input.ToPeer, input.SenderUserId,
                replyToMsgId));
        var idType = IdType.MessageId;
        var subType = MessageSubType.Normal;
        var messageAction = MessageActionType.None;
        var post = channelReadModel?.Broadcast ?? false;
        var linkedChannelId = channelReadModel?.Broadcast ?? false ? channelReadModel.LinkedChatId : null;
        string? postAuthor = null;

        if (post && channelReadModel!.Signatures)
        {
            if (input.SendAs?.PeerType != PeerType.Channel)
            {
                var user = await userAppService.GetAsync(input.SendAs?.PeerId ?? input.RequestInfo.UserId);
                postAuthor = $"{user!.FirstName} {user.LastName}";
            }
        }

        var pts = await idGenerator.NextIdAsync(IdType.Pts, ownerPeerId); ;

        MessageReply? reply = null;
        if (post && linkedChannelId.HasValue)
        {
            reply = new MessageReply(linkedChannelId, 0, 0, 0, []);
        }

        var messageId = await idGenerator.NextIdAsync(idType, ownerPeerId);

        var date = CurrentDate;
        var messageItem = new MessageItem(
            input.ToPeer with { PeerId = ownerPeerId /*, AccessHash = 0 */ },
            input.ToPeer,
            new Peer(PeerType.User, input.SenderUserId),
            input.SenderUserId,
            messageId,
            input.Message,
            date,
            input.RandomId,
            true,
            input.SendMessageType,
            (MessageType)input.SendMessageType,
            subType,
            input.InputReplyTo,
            input.MessageActionData,
            messageAction,
            item.entities,
            input.Media,
            input.GroupId,
            PollId: input.PollId,
            Post: post,
            ReplyMarkup: input.ReplyMarkup,
            TopMsgId: input.TopMsgId,
            PostAuthor: postAuthor,
            SendAs: input.SendAs,
            Effect: input.Effect,
            ReplyToMsgItems: replyToMsgItems?.ToList(),
            LinkedChannelId: linkedChannelId,
            Pts: pts,
            Silent: input.Silent,
            Reply: reply,
            InvertMedia: input.InvertMedia
        );

        var sendMessageItem = new SendMessageItem(messageItem, input.ClearDraft, item.mentionedUserIds, chatMembers);

        return sendMessageItem;
    }

    private (List<TMessageEntityMention> mentions, List<string> userNameList) GetMentions(string message)
    {
        var pattern = "@(\\w{4,40})";
        var mentions = new List<TMessageEntityMention>();
        var matches = Regex.Matches(message, pattern);
        var userNameList = new List<string>();
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                mentions.Add(new TMessageEntityMention
                {
                    Offset = match.Index,
                    Length = match.Length
                });
                userNameList.Add(match.Value[1..]);
            }
        }

        return (mentions, userNameList);
    }

    private Task<GetMessageOutput> GetMessagesCoreAsync<TRequest>(TRequest input)
        where TRequest : GetPagedListInput
    {
        var offset = offsetHelper.GetOffsetInfo(input);
        var query = objectMapper.Map<TRequest, GetMessagesQuery>(input);
        query.Offset = offset;

        return GetMessagesInternalAsync(query);
    }

    private async Task<GetMessageOutput> GetMessagesInternalAsync(GetMessagesQuery query, IReadOnlyCollection<long>? users = null,
        IReadOnlyCollection<long>? chats = null)
    {
        var messageList = await queryProcessor.ProcessAsync(query);
        var chatOrChannelPeers = chats?.Count > 0 ? chats.Select(peerHelper.GetPeer).ToList() : new List<Peer>(0);
        var userIdList = messageList.Where(p => p.ToPeerType == PeerType.User).Select(p => p.ToPeerId).ToList();
        var chatIdList = messageList.Where(p => p.ToPeerType == PeerType.Chat).Select(p => p.ToPeerId).ToList();
        var channelIdList = messageList.Where(p => p.ToPeerType == PeerType.Channel).Select(p => p.ToPeerId).ToList();
        var extraChatUserIdList = new List<long>();

        if (users?.Count > 0)
        {
            extraChatUserIdList.AddRange(users);
        }

        foreach (var messageReadModel in messageList)
        {
            switch (messageReadModel.MessageActionType)
            {
                case MessageActionType.ChatAddUser:
                    var messageActionData = messageReadModel.MessageActionData!.ToBytes()
                        .ToTObject<IObject>();
                    switch (messageActionData)
                    {
                        case TMessageActionChatAddUser messageActionChatAddUser:
                            extraChatUserIdList.AddRange(messageActionChatAddUser.Users);
                            break;

                        case TMessageActionChatJoinedByLink messageActionChatJoinedByLink:
                            extraChatUserIdList.Add(messageActionChatJoinedByLink.InviterId);
                            break;

                        case TMessageActionChatJoinedByRequest:

                            break;
                    }

                    break;
                case MessageActionType.ChatDeleteUser:
                    var deletedUserId = messageReadModel.MessageActionData!.ToBytes()
                        .ToTObject<TMessageActionChatDeleteUser>()
                        .UserId;
                    extraChatUserIdList.Add(deletedUserId);
                    break;
            }

            var fwd = messageReadModel.FwdHeader;
            AddPeerIdIfNeed(fwd?.FromId, userIdList, channelIdList);
            AddPeerIdIfNeed(fwd?.SavedFromId, userIdList, channelIdList);
            AddPeerIdIfNeed(fwd?.SavedFromPeer, userIdList, channelIdList);

            extraChatUserIdList.Add(messageReadModel.SenderPeerId);
        }

        userIdList.Add(query.SelfUserId);
        userIdList.AddRange(extraChatUserIdList);

        if (chatOrChannelPeers.Count > 0)
        {
            chatIdList.AddRange(chatOrChannelPeers.Where(p => p.PeerType == PeerType.Chat).Select(p => p.PeerId));
            channelIdList.AddRange(chatOrChannelPeers.Where(p => p.PeerType == PeerType.Channel).Select(p => p.PeerId));
        }

        var userList = await queryProcessor.ProcessAsync(new GetUsersByUserIdListQuery(userIdList));
        var chatList = chatIdList.Count == 0
            ? new List<IChatReadModel>()
            : await queryProcessor
                .ProcessAsync(new GetChatByChatIdListQuery(chatIdList));

        var channelList = channelIdList.Count == 0
                ? new List<IChannelReadModel>()
                : await queryProcessor
                    .ProcessAsync(new GetChannelByChannelIdListQuery(channelIdList));

        var contactList = await queryProcessor
                .ProcessAsync(new GetContactListQuery(query.SelfUserId, userIdList));

        var photoIds = new List<long>();
        photoIds.AddRange(chatList.Select(p => p.PhotoId ?? 0));
        photoIds.AddRange(channelList.Select(p => p.PhotoId ?? 0));
        photoIds.AddRange(userList.Select(p => p.ProfilePhotoId ?? 0));
        photoIds.AddRange(userList.Select(p => p.FallbackPhotoId ?? 0));
        photoIds.AddRange(contactList.Select(p => p.PhotoId ?? 0));
        photoIds.RemoveAll(p => p == 0);

        var photoList = await photoAppService.GetListAsync(photoIds);

        IReadOnlyCollection<long> joinedChannelIdList = new List<long>();
        if (channelIdList.Count > 0)
        {
            joinedChannelIdList = await queryProcessor
                .ProcessAsync(new GetJoinedChannelIdListQuery(query.SelfUserId, channelIdList));
        }

        var privacyList = await privacyAppService.GetPrivacyListAsync(userIdList);
        IReadOnlyCollection<IChannelMemberReadModel> channelMemberList = new List<IChannelMemberReadModel>();
        if (joinedChannelIdList.Count > 0)
        {
            channelMemberList = await queryProcessor
                .ProcessAsync(
                    new GetChannelMemberListByChannelIdListQuery(query.SelfUserId, joinedChannelIdList.ToList()));
        }

        var pts = query.Pts;
        if (pts == 0 && messageList.Count > 0)
        {
            pts = messageList.Max(p => p.Pts);
        }

        var pollIdList = messageList.Where(p => p.PollId.HasValue).Select(p => p.PollId!.Value).ToList();
        IReadOnlyCollection<IPollReadModel>? pollReadModels = null;
        IReadOnlyCollection<IPollAnswerVoterReadModel>? chosenOptions = null;

        if (pollIdList.Count > 0)
        {
            pollReadModels = await queryProcessor.ProcessAsync(new GetPollsQuery(pollIdList));
            chosenOptions = await queryProcessor
                .ProcessAsync(new GetChosenVoteAnswersQuery(pollIdList, query.SelfUserId));
        }

        //var channelPostIdList=messageList.Where(p=>p.Post)

        return new GetMessageOutput(channelList,
            channelMemberList,
            chatList,
            contactList,
            joinedChannelIdList,
            messageList,
            privacyList,
            userList,
            photoList,
            pollReadModels,
            chosenOptions,
            query.Limit == messageList.Count,
            query.IsSearchGlobal,
            pts,
            query.SelfUserId,
            query.Limit,
            query.Offset
        );
    }

    private void AddPeerIdIfNeed(Peer? peer, List<long> userIds, List<long> channelIds)
    {
        switch (peer?.PeerType)
        {
            case PeerType.Channel:
                channelIds.Add(peer.PeerId);
                break;
            case PeerType.User:
                userIds.Add(peer.PeerId);
                break;
        }
    }
}