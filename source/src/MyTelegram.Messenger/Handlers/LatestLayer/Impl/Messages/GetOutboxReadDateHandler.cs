// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getOutboxReadDate" />
///</summary>
internal sealed class GetOutboxReadDateHandler(
    IQueryProcessor queryProcessor,
    IUserAppService userAppService,
    IPeerHelper peerHelper,
    IPrivacyAppService privacyAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetOutboxReadDate, MyTelegram.Schema.IOutboxReadDate>,
        Messages.IGetOutboxReadDateHandler
{
    protected override async Task<MyTelegram.Schema.IOutboxReadDate> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetOutboxReadDate obj)
    {
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        if (peer.PeerType == PeerType.User)
        {
            var userReadModel = await userAppService.GetAsync(input.UserId);
            if (!userReadModel?.Premium ?? false)
            {
                var selfPrivacy = await privacyAppService.GetGlobalPrivacySettingsAsync(input.UserId);
                if (selfPrivacy?.HideReadMarks ?? false)
                {
                    RpcErrors.RpcErrors403.YourPrivacyRestricted.ThrowRpcError();
                }

                var toPeerPrivacy = await privacyAppService.GetGlobalPrivacySettingsAsync(peer.PeerId);
                if (toPeerPrivacy?.HideReadMarks ?? false)
                {
                    RpcErrors.RpcErrors403.UserPrivacyRestricted.ThrowRpcError();
                }
            }
        }

        var date = await queryProcessor.ProcessAsync(new GetOutboxReadDateQuery(input.UserId, obj.MsgId, peer));
        var diff = CurrentDate - date;
        if (diff > MyTelegramServerDomainConsts.ChatReadMarkExpirePeriod)
        {
            RpcErrors.RpcErrors400.MessageTooOld.ThrowRpcError();
        }

        return new TOutboxReadDate
        {
            Date = date
        };
    }
}
