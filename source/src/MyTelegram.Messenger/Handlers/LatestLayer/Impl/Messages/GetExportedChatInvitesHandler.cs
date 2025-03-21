// ReSharper disable All

using MyTelegram.Messenger.Converters.ConverterServices;
using IExportedChatInvite = MyTelegram.Schema.IExportedChatInvite;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Get info about the chat invites of a specific chat
/// <para>Possible errors</para>
/// Code Type Description
/// 400 ADMIN_ID_INVALID The specified admin ID is invalid.
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getExportedChatInvites" />
///</summary>
internal sealed class GetExportedChatInvitesHandler(
    IPeerHelper peerHelper,
    IOptions<MyTelegramMessengerServerOptions> options,
    IAccessHashHelper accessHashHelper,
    IQueryProcessor queryProcessor,
    IObjectMapper objectMapper,
    IChannelAppService channelAppService,
    IPhotoAppService photoAppService,
    IPrivacyAppService privacyAppService,
    IUserAppService userAppService,
    //ILayeredService<IUserConverter> layeredUserService,
    IUserConverterService userConverterService,
    IChatInviteLinkHelper chatInviteLinkHelper)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetExportedChatInvites,
            MyTelegram.Schema.Messages.IExportedChatInvites>,
        Messages.IGetExportedChatInvitesHandler
{
    protected async override Task<MyTelegram.Schema.Messages.IExportedChatInvites> HandleCoreAsync(IRequestInput input,
        RequestGetExportedChatInvites obj)
    {
        await accessHashHelper.CheckAccessHashAsync(obj.Peer);
        await accessHashHelper.CheckAccessHashAsync(obj.AdminId);

        // todo:impl get chat invites
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var channelReadModel = await channelAppService.GetAsync(peer.PeerId);
        if (channelReadModel == null)
        {
            RpcErrors.RpcErrors400.PeerIdInvalid.ThrowRpcError();
        }

        if (!channelReadModel!.AdminList.Any(p => p.UserId == input.UserId))
        {
            RpcErrors.RpcErrors400.ChatAdminRequired.ThrowRpcError();
        }

        var admin = peerHelper.GetPeer(obj.AdminId, input.UserId);

        var invites = await queryProcessor
            .ProcessAsync(new GetChatInvitesQuery(obj.Revoked,
                    peer.PeerId,
                    admin.PeerId,
                    obj.OffsetDate,
                    obj.OffsetLink,
                    obj.Limit));
        var userIds = invites.Select(p => p.AdminId).ToList();
        var users = await userConverterService.GetUserListAsync(input.UserId, userIds, false, false, input.Layer);

        var tInvites = invites.Select(p => objectMapper.Map<IChatInviteReadModel, TChatInviteExported>(p)).ToList();
        tInvites.ForEach(p => p.Link = chatInviteLinkHelper.GetFullLink(options.Value.JoinChatDomain, p.Link));

        var r = new TExportedChatInvites
        {
            Count = invites.Count,
            Invites = new TVector<IExportedChatInvite>(tInvites),
            Users = new TVector<IUser>(users),
        };

        return r;
    }
}
