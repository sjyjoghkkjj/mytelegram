namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Get the participants of a <a href="https://corefork.telegram.org/api/channel">supergroup/channel</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 406 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 403 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// See <a href="https://corefork.telegram.org/method/channels.getParticipants" />
///</summary>
internal sealed class GetParticipantsHandler(
    IQueryProcessor queryProcessor,
    IChatConverterService chatConverterService,
    IAccessHashHelper accessHashHelper,
    IUserConverterService userConverterService,
    IPhotoAppService photoAppService,
    IChannelAdminRightsChecker channelAdminRightsChecker,
    IChannelAppService channelAppService)
    : RpcResultObjectHandler<RequestGetParticipants,
            IChannelParticipants>,
        IGetParticipantsHandler
{
    protected override async Task<IChannelParticipants> HandleCoreAsync(IRequestInput input,
        RequestGetParticipants obj)
    {
        if (obj.Channel is TInputChannel inputChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(input, inputChannel.ChannelId, inputChannel.AccessHash, AccessHashType.Channel);
            var channelReadModel = await channelAppService.GetAsync(inputChannel.ChannelId);
            channelReadModel.ThrowExceptionIfChannelDeleted();
            if (channelReadModel.ParticipantsHidden)
            {
                if (!await channelAdminRightsChecker.HasChatAdminRightAsync(inputChannel.ChannelId, input.UserId,
                        p => p.IsCreator || p.AdminRights.ChangeInfo))
                {
                    return new TChannelParticipants
                    {
                        Chats = [],
                        Count = 0,
                        Participants = [],
                        Users = []
                    };
                }
            }

            var joinedChannelIdList = await queryProcessor.ProcessAsync(new GetJoinedChannelIdListQuery(input.UserId,
                    [inputChannel.ChannelId]));

            if (joinedChannelIdList.Count == 0 && channelReadModel.Broadcast)
            {
                return new TChannelParticipants
                {
                    Chats = [],
                    Count = 0,
                    Participants = [],
                    Users = []
                };
            }

            void CheckAdminPermission(IChannelReadModel channel,
                long userId)
            {
                if (channelReadModel.Broadcast)
                {
                    if (channel.CreatorId != userId &&
                        channel.AdminList?.FirstOrDefault(p => p.UserId == userId) == null)
                    {
                        RpcErrors.RpcErrors403.ChatAdminRequired.ThrowRpcError();
                    }
                }
            }

            var forceNotLeft = false;
            IReadOnlyCollection<IChatAdminReadModel>? chatAdminReadModels = null;
            IQuery<IReadOnlyCollection<IChannelMemberReadModel>>? query = null;
            switch (obj.Filter)
            {
                case TChannelParticipantsAdmins:
                    chatAdminReadModels = await queryProcessor.ProcessAsync(
                        new GetChatAdminListByChannelIdQuery(inputChannel.ChannelId, obj.Offset, obj.Limit));

                    break;
                case TChannelParticipantsBots:
                    return new TChannelParticipants
                    {
                        Participants = [],
                        Users = [],
                        Chats = []
                    };
                case TChannelParticipantsKicked:
                    CheckAdminPermission(channelReadModel!, input.UserId);
                    forceNotLeft = true;
                    query = new GetKickedChannelMembersQuery(inputChannel.ChannelId, obj.Offset, obj.Limit);
                    break;
                default:
                    query = new GetChannelMembersByChannelIdQuery(inputChannel.ChannelId,
                        [],
                        obj.Offset,
                        obj.Limit);
                    break;
            }

            if (joinedChannelIdList.Contains(channelReadModel.ChannelId))
            {
                forceNotLeft = true;
            }

            var channelMemberReadModels = query == null ? [] : await queryProcessor
                .ProcessAsync(query);

            if (channelMemberReadModels.Count == 0)
            {
                return new TChannelParticipants
                {
                    Chats = [],
                    Count = 0,
                    Participants = [],
                    Users = []
                };
            }


            var userIdList = channelMemberReadModels.Select(p => p.UserId).ToList();
            var selfChannelMember = channelMemberReadModels.FirstOrDefault(p => p.UserId == input.UserId);
            if (selfChannelMember != null)
            {
                userIdList.Add(selfChannelMember.InviterId);
            }

            var users = await userConverterService.GetUserListAsync(input, userIdList, false, false, input.Layer);

            var chatPhoto = await photoAppService.GetAsync(channelReadModel.PhotoId);

            var creatorId = channelReadModel.CreatorId;
            if (channelReadModel.Broadcast || (channelReadModel.HasLink && input.UserId != creatorId))
            {
                if (channelMemberReadModels.Count > 0)
                {
                    var newChannelMemberReadModels = channelMemberReadModels.ToList();
                    newChannelMemberReadModels.RemoveAll(p => p.UserId == creatorId);
                    channelMemberReadModels = newChannelMemberReadModels;
                }

                if (users.Count > 0)
                {
                    var newUsers = users.ToList();
                    newUsers.RemoveAll(p => p.Id == creatorId);
                    users = newUsers;
                }
            }

            if (chatAdminReadModels?.Count == 0 && channelMemberReadModels.Count == 0)
            {
                return new TChannelParticipants
                {
                    Chats = [],
                    Participants = [],
                    Users = [],
                };
            }

            return chatConverterService.ToChannelParticipants(
                input,
                channelReadModel,
                chatPhoto,
                chatAdminReadModels,
                channelMemberReadModels,
                users,
                DeviceType.Unknown,
                forceNotLeft,
                input.Layer
                );
        }

        throw new NotImplementedException();
    }
}
