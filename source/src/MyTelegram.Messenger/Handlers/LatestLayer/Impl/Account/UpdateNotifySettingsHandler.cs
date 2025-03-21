// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Edits notification settings from a given user/group, from all users/all groups.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 400 SETTINGS_INVALID Invalid settings were provided.
/// See <a href="https://corefork.telegram.org/method/account.updateNotifySettings" />
///</summary>
internal sealed class UpdateNotifySettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateNotifySettings, IBool>,
    Account.IUpdateNotifySettingsHandler
{
    private readonly ICommandBus _commandBus;
    private readonly IPeerHelper _peerHelper;

    public UpdateNotifySettingsHandler(ICommandBus commandBus,
        IPeerHelper peerHelper)
    {
        _commandBus = commandBus;
        _peerHelper = peerHelper;
    }

    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        RequestUpdateNotifySettings obj)
    {
        if (obj.Peer is TInputNotifyPeer inputNotifyPeer)
        {
            var userId = input.UserId;
            var targetPeer = _peerHelper.GetPeer(inputNotifyPeer.Peer, userId);
            var aggregateId = PeerNotifySettingsId.Create(userId, targetPeer.PeerType, targetPeer.PeerId);
            var updatePeerNotifySettingsCommand = new UpdatePeerNotifySettingsCommand(aggregateId,
                input.ToRequestInfo(), 
                input.UserId,
                targetPeer.PeerType,
                targetPeer.PeerId,
                obj.Settings.ShowPreviews,
                obj.Settings.Silent,
                obj.Settings.MuteUntil,
                string.Empty
                //obj.Settings.Sound
            );
            await _commandBus.PublishAsync(updatePeerNotifySettingsCommand, CancellationToken.None)
         ;
            return null!;
            //return new TBoolTrue();
        }

        throw new NotImplementedException();
    }
}
