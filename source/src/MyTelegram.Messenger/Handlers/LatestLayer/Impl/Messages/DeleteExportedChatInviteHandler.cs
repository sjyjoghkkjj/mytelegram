// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Delete a chat invite
/// <para>Possible errors</para>
/// Code Type Description
/// 400 INVITE_HASH_EXPIRED The invite link has expired.
/// 400 INVITE_REVOKED_MISSING The specified invite link was already revoked or is invalid.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.deleteExportedChatInvite" />
///</summary>
internal sealed class DeleteExportedChatInviteHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestDeleteExportedChatInvite, IBool>,
    Messages.IDeleteExportedChatInviteHandler
{
    private readonly IQueryProcessor _queryProcessor;
    private readonly IAccessHashHelper _accessHashHelper;
    private readonly ICommandBus _commandBus;
    private readonly IChannelAdminRightsChecker _channelAdminRightsChecker;
    private readonly IChatInviteLinkHelper _chatInviteLinkHelper;
    public DeleteExportedChatInviteHandler(IQueryProcessor queryProcessor, IAccessHashHelper accessHashHelper, ICommandBus commandBus, IChannelAdminRightsChecker channelAdminRightsChecker, IChatInviteLinkHelper chatInviteLinkHelper)
    {
        _queryProcessor = queryProcessor;
        _accessHashHelper = accessHashHelper;
        _commandBus = commandBus;
        _channelAdminRightsChecker = channelAdminRightsChecker;
        _chatInviteLinkHelper = chatInviteLinkHelper;
    }

    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        RequestDeleteExportedChatInvite obj)
    {
        switch (obj.Peer)
        {
            case TInputPeerChannel inputPeerChannel:
                {
                    var link = _chatInviteLinkHelper.GetHashFromLink(obj.Link);
                    await _accessHashHelper.CheckAccessHashAsync(inputPeerChannel);
                    var chatInviteReadModel = await _queryProcessor.ProcessAsync(new GetChatInviteQuery(inputPeerChannel.ChannelId, link));
                    if (chatInviteReadModel == null)
                    {
                        RpcErrors.RpcErrors400.PeerIdInvalid.ThrowRpcError();
                    }

                    await _channelAdminRightsChecker.CheckAdminRightAsync(inputPeerChannel.ChannelId, input.UserId,
                        (p) => p.AdminRights.ChangeInfo, RpcErrors.RpcErrors403.ChatAdminRequired);

                    var command = new DeleteExportedInviteCommand(
                        ChatInviteId.Create(inputPeerChannel.ChannelId, chatInviteReadModel!.InviteId),
                        input.ToRequestInfo());
                    await _commandBus.PublishAsync(command, default);
                }
                break;

            case TInputPeerChat inputPeerChat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new TBoolTrue();
    }
}
