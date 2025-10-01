namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Get scheduled messages
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getScheduledHistory" />
///</summary>
internal sealed class GetScheduledHistoryHandler(IQueryProcessor queryProcessor, IAccessHashHelper accessHashHelper, IPeerHelper peerHelper, IMessageConverterService messageConverterService) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetScheduledHistory, MyTelegram.Schema.Messages.IMessages>
{
    protected override async Task<MyTelegram.Schema.Messages.IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetScheduledHistory obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var scheduled = await queryProcessor.ProcessAsync(new GetScheduledMessagesQuery(input.UserId, peer.PeerId));
        var messages = scheduled
            .Select(s => messageConverterService.ToMessage(input.UserId, s.Item, s.MessageId, layer: input.Layer))
            .ToList();
        return new TMessages
        {
            Chats = [],
            Messages = new TVector<IMessage>(messages),
            Users = []
        };
    }
}
