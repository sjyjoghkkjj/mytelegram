namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Sends a service message to a secret chat.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 DATA_INVALID Encrypted data invalid.
/// 400 ENCRYPTION_DECLINED The secret chat was declined.
/// 400 ENCRYPTION_ID_INVALID The provided secret chat ID is invalid.
/// 500 MSG_WAIT_FAILED A waiting call returned an error.
/// 403 USER_DELETED You can't send this secret message because the other participant deleted their account.
/// 403 USER_IS_BLOCKED You were blocked by this user.
/// See <a href="https://corefork.telegram.org/method/messages.sendEncryptedService" />
///</summary>
internal sealed class SendEncryptedServiceHandler(ISecretChatService secretChats, IResponseCacheAppService responseCache) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendEncryptedService, MyTelegram.Schema.Messages.ISentEncryptedMessage>
{
    protected override Task<MyTelegram.Schema.Messages.ISentEncryptedMessage> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendEncryptedService obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        var rid = secretChats.AddMessage(chatId, input.UserId, obj.Data, hasFile: false);
        var state = secretChats.Get(chatId)!;
        foreach (var u in secretChats.BuildPendingUpdates(state.AdminId == input.UserId ? state.ParticipantId : state.AdminId))
        {
            responseCache.AddToCache(input.ReqMsgId, u);
        }
        return Task.FromResult<MyTelegram.Schema.Messages.ISentEncryptedMessage>(new MyTelegram.Schema.Messages.TSentEncryptedMessage
        {
            Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            RandomId = rid
        });
    }
}
