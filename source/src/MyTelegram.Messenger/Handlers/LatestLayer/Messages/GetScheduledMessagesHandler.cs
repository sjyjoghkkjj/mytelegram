namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Get scheduled messages
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getScheduledMessages" />
///</summary>
internal sealed class GetScheduledMessagesHandler(IQueryProcessor queryProcessor, IAccessHashHelper accessHashHelper, IPeerHelper peerHelper, IMessageConverterService messageConverterService) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetScheduledMessages, MyTelegram.Schema.Messages.IMessages>
{
    protected override async Task<MyTelegram.Schema.Messages.IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetScheduledMessages obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var scheduled = await queryProcessor.ProcessAsync(new GetScheduledMessagesQuery(input.UserId, peer.PeerId));
        var messages = scheduled
            .Where(s => obj.Id.Contains(s.MessageId))
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
