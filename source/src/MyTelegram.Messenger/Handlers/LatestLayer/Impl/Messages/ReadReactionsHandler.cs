// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Mark <a href="https://corefork.telegram.org/api/reactions">message reactions »</a> as read
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.readReactions" />
///</summary>
internal sealed class ReadReactionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestReadReactions, MyTelegram.Schema.Messages.IAffectedHistory>,
    Messages.IReadReactionsHandler
{
    private readonly IPtsHelper _ptsHelper;
    private readonly IPeerHelper _peerHelper;
    private readonly IAccessHashHelper _accessHashHelper;
    private readonly IQueryProcessor _queryProcessor;
    public ReadReactionsHandler(IPtsHelper ptsHelper, IPeerHelper peerHelper, IAccessHashHelper accessHashHelper, IQueryProcessor queryProcessor)
    {
        _ptsHelper = ptsHelper;
        _peerHelper = peerHelper;
        _accessHashHelper = accessHashHelper;
        _queryProcessor = queryProcessor;
    }

    protected override async Task<MyTelegram.Schema.Messages.IAffectedHistory> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestReadReactions obj)
    {
        var peer = _peerHelper.GetPeer(obj.Peer, input.UserId);
        await _accessHashHelper.CheckAccessHashAsync(obj.Peer);

        return new TAffectedHistory
        {
            Pts = _ptsHelper.GetCachedPts(peer.PeerId),
            PtsCount = 0
        };
    }
}
