namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Sends a text message to a secret chat.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// 400 DATA_INVALID Encrypted data invalid.
/// 400 DATA_TOO_LONG Data too long.
/// 400 ENCRYPTION_DECLINED The secret chat was declined.
/// 400 MSG_WAIT_FAILED A waiting call returned an error.
/// 403 USER_IS_BLOCKED You were blocked by this user.
/// See <a href="https://corefork.telegram.org/method/messages.sendEncrypted" />
///</summary>
internal sealed class SendEncryptedHandler(ISecretChatService secretChats, IResponseCacheAppService responseCache, ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendEncrypted, MyTelegram.Schema.Messages.ISentEncryptedMessage>
{
    protected override Task<MyTelegram.Schema.Messages.ISentEncryptedMessage> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendEncrypted obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        var state = secretChats.Get(chatId);
        if (state == null)
        {
            RpcErrors.RpcErrors400.ChatIdInvalid.ThrowRpcError();
        }
        var rid = secretChats.AddMessage(chatId, input.UserId, obj.Data, hasFile: false);
        // Persist encrypted push update with QTS increment (simplified: qts=0 placeholder)
        var toUserId = state.AdminId == input.UserId ? state.ParticipantId : state.AdminId;
        var updatesBytes = new MyTelegram.Schema.TUpdateNewEncryptedMessage
        {
            Message = new MyTelegram.Schema.TEncryptedMessage
            {
                ChatId = chatId,
                Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RandomId = rid,
                Bytes = obj.Data
            },
            Qts = 0
        }.ToBytes();
        commandBus.PublishAsync(new MyTelegram.Domain.Commands.PushUpdates.CreateEncryptedPushUpdatesCommand(new MyTelegram.Domain.Aggregates.PushUpdates.PushUpdatesId(toUserId), toUserId, updatesBytes, 0, input.PermAuthKeyId!.Value)).GetAwaiter().GetResult();
        return Task.FromResult<MyTelegram.Schema.Messages.ISentEncryptedMessage>(new MyTelegram.Schema.Messages.TSentEncryptedMessage
        {
            Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            RandomId = rid
        });
    }
}
