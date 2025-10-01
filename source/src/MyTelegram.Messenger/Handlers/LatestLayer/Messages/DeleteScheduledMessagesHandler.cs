namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Delete scheduled messages
/// See <a href="https://corefork.telegram.org/method/messages.deleteScheduledMessages" />
///</summary>
internal sealed class DeleteScheduledMessagesHandler(ICommandBus commandBus, IAccessHashHelper accessHashHelper, IPeerHelper peerHelper, IResponseCacheAppService responseCache) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestDeleteScheduledMessages, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestDeleteScheduledMessages obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        foreach (var id in obj.Id)
        {
            await commandBus.PublishAsync(new CancelScheduledMessageCommand(MessageId.Create(peer.PeerId, id), input.ToRequestInfo(), 0));
        }
        // Push updateDeleteScheduledMessages
        responseCache.AddToCache(input.ReqMsgId, new TUpdateDeleteScheduledMessages
        {
            Peer = peer.ToPeer(),
            Id = new TVector<int>(obj.Id)
        });
        return new TUpdates
        {
            Updates = [],
            Chats = [],
            Users = [],
            Date = CurrentDate
        };
    }
}
