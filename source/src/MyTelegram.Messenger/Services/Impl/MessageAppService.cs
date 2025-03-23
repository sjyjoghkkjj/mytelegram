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
    IContactAppService contactAppService,
    IOffsetHelper offsetHelper,
    IIdGenerator idGenerator,
    IReadModelCacheHelper<IUserReadModel> useReadModelCacheHelper)
    : BaseAppService, IMessageAppService, ITransientDependency
{
    public void CheckBotPermission(long requestUserId, Peer toPeer)
    {
        if (peerHelper.IsBotUser(requestUserId) && peerHelper.IsBotUser(toPeer.PeerId))
        {
            RpcErrors.RpcErrors400.UserIsBot.ThrowRpcError();
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
                    var sendToChannelReadModel = await channelAppService.GetAsync(toPeer.PeerId);
                    // 1. Super group with linked channel
                    // 2. Channel: signature: true 
                    // 3. Linked private channel
                    if (sendToChannelReadModel is not ({ MegaGroup: true, LinkedChatId: not null } or
                        { Broadcast: true, Signatures: true }))
                    {
                        RpcErrors.RpcErrors400.SendAsPeerInvalid.ThrowRpcError();
                    }

                    var sendAsChannelReadModel = await channelAppService.GetAsync(sendAs.PeerId);

                    // We can only use the public channels created by the current user as SendAs
                    if (sendAsChannelReadModel == null! ||
                        sendAsChannelReadModel.CreatorId != requestUserId ||
                        (string.IsNullOrEmpty(sendAsChannelReadModel.UserName) &&
                         sendAsChannelReadModel.LinkedChatId != sendToChannelReadModel.ChannelId &&
                         sendAsChannelReadModel.ChannelId != toPeer.PeerId))
                    {
                        RpcErrors.RpcErrors400.SendAsPeerInvalid.ThrowRpcError();
                    }
                    break;
            }
        }
    }

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
            CheckBotPermission(input.RequestInfo.UserId, input.ToPeer);
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
            if (channelReadModel is { Broadcast: false, LinkedChatId: not null, JoinToSend: true })
            {

            }
            else
            {
                RpcErrors.RpcErrors403.ChatGuestSendForbidden.ThrowRpcError();
            }
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

    private Task CheckSendAsAsync(SendMessageInput input)
    {
        return CheckSendAsAsync(input.RequestInfo.UserId, input.ToPeer, input.SendAs);
    }

    private async Task<SendMessageItem> CreateSendMessageItemAsync(SendMessageInput input)
    {
        //await CheckAccessHashAsync(input);
        await CheckSendAsAsync(input);
        await CheckGlobalPrivacySettingsAsync(input);
        var channelReadModel = await CheckChannelBannedRightsAsync(input);

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
        var messageActionType = MessageActionType.None;
        var post = channelReadModel?.Broadcast ?? false;
        var linkedChannelId = channelReadModel?.Broadcast ?? false ? channelReadModel.LinkedChatId : null;
        var sendAs = input.SendAs;
        string? postAuthor = null;
        if (channelReadModel is { Signatures: true, Broadcast: true })
        {
            if (sendAs?.PeerType == PeerType.Channel)
            {
                var sendAsChannelReadModel = await channelAppService.GetAsync(sendAs?.PeerId);
                postAuthor = sendAsChannelReadModel?.Title;
            }
            else
            {
                var userReadModel = await userAppService.GetAsync(input.RequestInfo.UserId);
                postAuthor = $"{userReadModel.FirstName} {userReadModel.LastName}";
            }

            if (sendAs == null && channelReadModel.SignatureProfiles)
            {
                sendAs = input.RequestInfo.UserId.ToUserPeer();
            }
        }

        var scheduleDate = input.ScheduleDate;
        if (scheduleDate.HasValue)
        {
            // If the schedule_date is less than 20 seconds in the future, the message will be sent immediately,
            // generating a normal updateNewMessage/updateNewChannelMessage.
            if (scheduleDate.Value - CurrentDate < 20)
            {
                scheduleDate = null;
            }
            else
            {
                idType = IdType.ScheduleMessageId;
            }
        }

        var pts = 0;
        MessageReply? reply = null;
        if (post && linkedChannelId.HasValue)
        {
            reply = new MessageReply(linkedChannelId, 0, 0, 0, []);
        }

        var messageId = await idGenerator.NextIdAsync(idType, ownerPeerId);
        //var messageId = 0;
        int? scheduleMessageId = null;
        if (idType == IdType.ScheduleMessageId)
        {
            scheduleMessageId = await idGenerator.NextIdAsync(IdType.ScheduleMessageId, ownerPeerId);
        }

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
            //input.MessageActionData,
            input.MessageAction,
            messageActionType,
            item.entities,
            input.Media,
            input.GroupId,
            PollId: input.PollId,
            Post: post,
            ReplyMarkup: input.ReplyMarkup,
            TopMsgId: input.TopMsgId,
            PostAuthor: postAuthor,
            SendAs: sendAs,
            Effect: input.Effect,
            ReplyToMsgItems: replyToMsgItems?.ToList(),
            LinkedChannelId: linkedChannelId,
            Pts: pts,
            Silent: input.Silent,
            ScheduleDate: scheduleDate,
            ScheduleMessageId: scheduleMessageId,
            Reply: reply,
            InvertMedia: input.InvertMedia
        );

        var sendMessageItem = new SendMessageItem(messageItem, input.ClearDraft, item.mentionedUserIds, []);

        return sendMessageItem;
    }

    private async Task CheckGlobalPrivacySettingsAsync(SendMessageInput input)
    {
        if (input.ToPeer.PeerType == PeerType.User && input.RequestInfo.UserId != input.ToPeer.PeerId)
        {
            var globalPrivacySettings = await privacyAppService.GetGlobalPrivacySettingsAsync(input.ToPeer.PeerId);
            if (globalPrivacySettings?.NewNoncontactPeersRequirePremium ?? false)
            {
                var userReadModel = await userAppService.GetAsync(input.RequestInfo.UserId);
                if (!userReadModel!.Premium)
                {
                    var contactType =
                        await contactAppService.GetContactTypeAsync(input.RequestInfo.UserId, input.ToPeer.PeerId);
                    if (contactType != ContactType.Mutual && contactType != ContactType.ContactOfTargetUser)
                    {
                        RpcErrors.RpcErrors406.PrivacyPremiumRequired.ThrowRpcError();
                    }
                }
            }
        }
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

    private async Task<(List<long> mentionedUserIds, TVector<IMessageEntity>? entities)> GetMessageEntitiesAsync(
        SendMessageInput input)
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

            entities ??= [];
            foreach (var messageEntityMention in mentions)
            {
                entities.Add(messageEntityMention);
            }
        }

        var pattern = @"(?:^|\s)(https?://[^\s]+)(?=\s|$)";
        var matches = Regex.Matches(input.Message, pattern);
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var entity = new TMessageEntityUrl
                {
                    Offset = match.Index,
                    Length = match.Length,
                };
                entities ??= [];
                entities.Add(entity);
            }
        }

        return (mentionedUserIds, entities);
    }

    private Task<GetMessageOutput> GetMessagesCoreAsync<TRequest>(TRequest input)
        where TRequest : GetPagedListInput
    {
        var offset = offsetHelper.GetOffsetInfo(input);
        var query = objectMapper.Map<TRequest, GetMessagesQuery>(input);
        query.Offset = offset;

        return GetMessagesInternalAsync(query);
    }

    private async Task<GetMessageOutput> GetMessagesInternalAsync(GetMessagesQuery query,
        IReadOnlyCollection<long>? users = null,
        IReadOnlyCollection<long>? chats = null)
    {
        var messageList = await queryProcessor.ProcessAsync(query);
        var channelIdList = messageList.Where(p => p.ToPeerType == PeerType.Channel).Select(p => p.ToPeerId).ToList();
        var userIdList = messageList.Where(p => p.ToPeerType == PeerType.User).Select(p => p.ToPeerId).ToList();
        var chatIdList = messageList.Where(p => p.ToPeerType == PeerType.Chat).Select(p => p.ToPeerId).ToList();

        var extraChatUserIdList = new List<long>();
        if (users?.Count > 0)
        {
            extraChatUserIdList.AddRange(users);
        }

        foreach (var messageReadModel in messageList)
        {
            switch (messageReadModel.MessageAction)
            {
                case TMessageActionChatAddUser messageActionChatAddUser:
                    extraChatUserIdList.AddRange(messageActionChatAddUser.Users);
                    break;

                case TMessageActionChatJoinedByLink messageActionChatJoinedByLink:
                    extraChatUserIdList.Add(messageActionChatJoinedByLink.InviterId);
                    break;

                case TMessageActionChatJoinedByRequest:

                    break;
                case TMessageActionChatDeleteUser messageActionChatDeleteUser:
                    extraChatUserIdList.Add(messageActionChatDeleteUser.UserId);
                    break;
            }

            var fwd = messageReadModel.FwdHeader;
            AddPeerIdIfNeed(fwd?.FromId, userIdList, channelIdList);
            AddPeerIdIfNeed(fwd?.SavedFromId, userIdList, channelIdList);
            AddPeerIdIfNeed(fwd?.SavedFromPeer, userIdList, channelIdList);
            AddPeerIdIfNeed(messageReadModel.SendAs, userIdList, channelIdList);

            extraChatUserIdList.Add(messageReadModel.SenderPeerId);
        }

        var chatOrChannelPeers = chats?.Count > 0 ? chats.Select(peerHelper.GetPeer).ToList() : [];

        userIdList.Add(query.SelfUserId);
        userIdList.AddRange(extraChatUserIdList);

        if (chatOrChannelPeers.Count > 0)
        {
            chatIdList.AddRange(chatOrChannelPeers.Where(p => p.PeerType == PeerType.Chat).Select(p => p.PeerId));
            channelIdList.AddRange(chatOrChannelPeers.Where(p => p.PeerType == PeerType.Channel).Select(p => p.PeerId));
        }

        var userList = await userAppService.GetListAsync(userIdList);
        var channelList = channelIdList.Count == 0
            ? new List<IChannelReadModel>()
            : await channelAppService.GetListAsync(channelIdList);

        var contactList = await queryProcessor
            .ProcessAsync(new GetContactListQuery(query.SelfUserId, userIdList));

        var photoIds = new List<long>();
        photoIds.AddRange(channelList.Select(p => p.PhotoId ?? 0));
        photoIds.AddRange(userList.Select(p => p.PersonalPhotoId ?? 0));
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

        return new GetMessageOutput(channelList,
            channelMemberList,
            [],
            contactList,
            joinedChannelIdList,
            messageList,
            privacyList,
            userList,
            photoList,
            pollReadModels,
            chosenOptions,
            [],
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