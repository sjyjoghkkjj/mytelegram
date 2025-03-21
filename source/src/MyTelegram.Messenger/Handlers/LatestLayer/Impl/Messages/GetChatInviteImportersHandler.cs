namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Get info about the users that joined the chat using a specific chat invite
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 INVITE_HASH_EXPIRED The invite link has expired.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 400 SEARCH_WITH_LINK_NOT_SUPPORTED You cannot provide a search query and an invite link at the same time.
/// See <a href="https://corefork.telegram.org/method/messages.getChatInviteImporters" />
///</summary>
internal sealed class GetChatInviteImportersHandler(
    IQueryProcessor queryProcessor,
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IUserConverterService userConverterService)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetChatInviteImporters,
            MyTelegram.Schema.Messages.IChatInviteImporters>,
        Messages.IGetChatInviteImportersHandler
{
    protected override async Task<MyTelegram.Schema.Messages.IChatInviteImporters> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetChatInviteImporters obj)
    {
        if (obj.Peer is TInputPeerChannel inputPeerChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(inputPeerChannel);
            var userPeer = peerHelper.GetPeer(obj.OffsetUser);

            var channelAdminReadModel = await queryProcessor.ProcessAsync(new GetChatAdminQuery(inputPeerChannel.ChannelId, input.UserId));
            if (channelAdminReadModel == null)
            {
                RpcErrors.RpcErrors403.ChatAdminRequired.ThrowRpcError();
            }

            var inviteImporterReadModels = await queryProcessor.ProcessAsync(
                new GetChatInviteImportersQuery(inputPeerChannel.ChannelId, obj.Requested ? ChatInviteRequestState.WaitingForApproval : null, 0, obj.OffsetDate,
                    userPeer.PeerId, obj.Q, obj.Limit));

            // only support layer 158+
            var importers = new List<TChatInviteImporter>();
            var userIds = new List<long>();
            foreach (var readModel in inviteImporterReadModels)
            {
                var importer = new TChatInviteImporter
                {
                    About = readModel.About,
                    ApprovedBy = readModel.ApprovedBy,
                    Date = readModel.Date,
                    Requested = readModel.ChatInviteRequestState == ChatInviteRequestState.WaitingForApproval,
                    UserId = readModel.UserId,
                    //ViaChatlist = readModel.ViaChatList
                };
                importers.Add(importer);
                userIds.Add(readModel.UserId);
            }

            var users = await userConverterService.GetUserListAsync(input.UserId, userIds, false, false, input.Layer);

            return new TChatInviteImporters
            {
                Importers = new(importers),
                Users = new(users),
            };
        }

        return new TChatInviteImporters
        {
            Importers = new(),
            Users = new(),
        };
    }
}
