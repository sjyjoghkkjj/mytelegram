namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Send scheduled messages right away
/// <para>Possible errors</para>
/// Code Type Description
/// 400 MESSAGE_ID_INVALID The provided message id is invalid.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.sendScheduledMessages" />
///</summary>
internal sealed class SendScheduledMessagesHandler(ICommandBus commandBus, IAccessHashHelper accessHashHelper, IPeerHelper peerHelper) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendScheduledMessages, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendScheduledMessages obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        foreach (var id in obj.Id)
        {
            await commandBus.PublishAsync(new CancelScheduledMessageCommand(MessageId.Create(peer.PeerId, id), input.ToRequestInfo(), 0));
        }
        return new TUpdates
        {
            Updates = [],
            Chats = [],
            Users = [],
            Date = CurrentDate
        };
    }
}
