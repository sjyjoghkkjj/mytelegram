namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Send scheduled messages right away
/// <para>Possible errors</para>
/// Code Type Description
/// 400 MESSAGE_ID_INVALID The provided message id is invalid.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.sendScheduledMessages" />
///</summary>
internal sealed class SendScheduledMessagesHandler(
    ICommandBus commandBus,
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IQueryProcessor queryProcessor,
    IMessageAppService messageAppService
    ) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendScheduledMessages, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendScheduledMessages obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);

        // Fetch scheduled messages from read model
        var scheduled = await queryProcessor.ProcessAsync(new GetScheduledMessagesQuery(input.UserId, peer.PeerId, obj.Id));

        // Build SendMessageInput list to send immediately (scheduleDate cleared so it becomes a normal send)
        var sendInputs = new List<SendMessageInput>();
        foreach (var s in scheduled)
        {
            var item = s.Item.MessageItem;
            var sendInput = new SendMessageInput(
                input.ToRequestInfo(),
                item.SenderUserId,
                item.ToPeer,
                item.Message,
                item.RandomId,
                entities: item.Entities,
                inputReplyTo: item.InputReplyTo,
                clearDraft: false,
                media: item.Media,
                sendMessageType: item.SendMessageType,
                messageType: item.MessageType,
                messageAction: item.MessageAction,
                groupId: item.GroupId,
                groupItemCount: 1,
                pollId: item.PollId,
                replyMarkup: item.ReplyMarkup,
                topMsgId: item.TopMsgId,
                sendAs: item.SendAs,
                inputQuickReplyShortcut: null,
                effect: item.Effect,
                isSendGroupedMessage: false,
                isSendQuickReplyMessage: false,
                silent: item.Silent,
                scheduleDate: null,
                invertMedia: item.InvertMedia
            );
            sendInputs.Add(sendInput);
        }

        if (sendInputs.Count > 0)
        {
            await messageAppService.SendMessageAsync(sendInputs);
        }

        // Cancel scheduled entries so they disappear from schedule box
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
