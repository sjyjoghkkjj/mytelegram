namespace MyTelegram.Messenger.Services.Impl;

public class DialogAppService(
    ICommandBus commandBus,
    IQueryProcessor queryProcessor,
    IPhotoAppService photoAppService,
    IPrivacyAppService privacyAppService,
    IChannelAppService channelAppService,
    IUserAppService userAppService,
    IPeerHelper peerHelper,
    IOffsetHelper offsetHelper)
    : BaseAppService, IDialogAppService, ITransientDependency
{
    public async Task ReorderPinnedDialogsAsync(ReorderPinnedDialogsInput input)
    {
        var order = 0;
        foreach (var peer in input.OrderedPeerList)
        {
            var command = new SetPinnedOrderCommand(DialogId.Create(input.SelfUserId, peer), order);
            await commandBus.PublishAsync(command, CancellationToken.None);
            order++;
        }
    }

    public async Task<GetDialogOutput> GetDialogsAsync(GetDialogInput input)
    {
        var offset = offsetHelper.GetOffsetInfo(input);
        DateTime? offsetDate = null;
        if (input.OffsetPeer != null && input.OffsetPeer.PeerType != PeerType.Empty)
        {
            var dialogId = DialogId.Create(input.OwnerId, input.OffsetPeer.PeerType, input.OffsetPeer.PeerId);
            var dialog = await queryProcessor.ProcessAsync(new GetDialogByIdQuery(dialogId));
            offsetDate = dialog?.CreationTime;
        }
        var query = new GetDialogsQuery(input.OwnerId,
            input.Pinned,
            offsetDate,
            offset,
            input.Limit,
            input.PeerIdList,
            input.FolderId
            );
        var dialogList = await queryProcessor.ProcessAsync(query);
        if (input.Pinned == true)
        {
            dialogList = dialogList.OrderBy(p => p.PinnedOrder).ToList();
        }

        var channelIdList = dialogList.Where(p => p.ToPeerType == PeerType.Channel).Select(p => p.ToPeerId)
            .ToList();
        if (input.PeerIdList?.Count > 0)
        {
            foreach (var peerId in input.PeerIdList)
            {
                if (peerHelper.IsChannelPeer(peerId))
                {
                    channelIdList.Add(peerId);
                }
            }
        }

        var channelList = channelIdList.Count == 0
            ? new List<IChannelReadModel>()
            : await channelAppService.GetListAsync(channelIdList);

        var topMessageIdList = dialogList.Where(p => p.ToPeerType != PeerType.Channel)
            .Select(p => MessageId.Create(p.OwnerId, p.TopMessage).Value).ToList();
        topMessageIdList.AddRange(channelList.Select(p => MessageId.Create(p.ChannelId, p.TopMessageId).Value));
        var minIdList = dialogList.Where(p => p.ChannelHistoryMinId > 0)
            .Select(p => MessageId.Create(input.OwnerId, p.ChannelHistoryMinId).Value).ToList();
        topMessageIdList.RemoveAll(minIdList.Contains);
        var messageReadModels =
            await queryProcessor.ProcessAsync(new GetMessagesByIdListQuery(topMessageIdList));

        var extraChatUserIdList = GetExtraChatUserIdList(messageReadModels);


        var chatIdList = messageReadModels.Where(p => p.ToPeerType == PeerType.Chat).Select(p => p.ToPeerId).ToList();
        var userIdList = messageReadModels.Where(p => p.ToPeerType == PeerType.User).Select(p => p.ToPeerId).ToList();

        if (dialogList.Count > 0 || messageReadModels.Count > 0)
        {
            userIdList.Add(input.OwnerId);
        }
        userIdList.AddRange(dialogList.Where(p => p.ToPeerType == PeerType.User).Select(p => p.ToPeerId));
        userIdList.AddRange(extraChatUserIdList);

        var userList = await userAppService.GetListAsync(userIdList);
        var contactList = await queryProcessor
            .ProcessAsync(new GetContactListQuery(input.OwnerId, userIdList));

        var privacyList = await privacyAppService.GetPrivacyListAsync(userIdList);
        // reset dialog top message box id
        var channelDict = channelList.ToDictionary(k => k.ChannelId, v => v);
        foreach (var dialogReadModel in dialogList)
        {
            if (dialogReadModel.ToPeerType == PeerType.Channel)
            {
                if (channelDict.TryGetValue(dialogReadModel.ToPeerId, out var channelReadModel))
                {
                    dialogReadModel.SetNewTopMessageId(channelReadModel.TopMessageId);
                }
            }
        }

        IReadOnlyCollection<IChannelMemberReadModel> channelMemberList = new List<IChannelMemberReadModel>();
        if (channelIdList.Count > 0)
        {
            channelMemberList = await queryProcessor
                .ProcessAsync(new GetChannelMemberListByChannelIdListQuery(input.OwnerId, channelIdList));
        }

        var pollIdList = messageReadModels.Where(p => p.PollId.HasValue).Select(p => p.PollId!.Value).ToList();
        IReadOnlyCollection<IPollReadModel>? pollReadModels = null;
        IReadOnlyCollection<IPollAnswerVoterReadModel>? chosenOptions = null;

        if (pollIdList.Count > 0)
        {
            pollReadModels =
                await queryProcessor.ProcessAsync(new GetPollsQuery(pollIdList));
            chosenOptions = await queryProcessor
                .ProcessAsync(new GetChosenVoteAnswersQuery(pollIdList, query.OwnerId));
        }

        var photoIds = new List<long>();
        photoIds.AddRange(channelList.Select(p => p.PhotoId ?? 0));
        photoIds.AddRange(userList.Select(p => p.ProfilePhotoId ?? 0));
        photoIds.AddRange(userList.Select(p => p.FallbackPhotoId ?? 0));
        photoIds.AddRange(contactList.Select(p => p.PhotoId ?? 0));
        photoIds.RemoveAll(p => p == 0);

        var photos = await photoAppService.GetListAsync(photoIds);

        return new GetDialogOutput(input.OwnerId,
            dialogList,
            messageReadModels,
            userList,
            photos,
            [],
            channelList,
            contactList,
            privacyList,
            channelMemberList,
            pollReadModels,
            chosenOptions,
            input.Limit
            );
    }

    private static List<long> GetExtraChatUserIdList(IReadOnlyCollection<IMessageReadModel> messageReadModels)
    {
        var extraChatUserIdList = new List<long>();
        foreach (var messageReadModel in messageReadModels)
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

            extraChatUserIdList.Add(messageReadModel.SenderPeerId);
        }

        return extraChatUserIdList;
    }
}