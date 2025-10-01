namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Sends a message with a file attachment to a secret chat
/// <para>Possible errors</para>
/// Code Type Description
/// 400 DATA_TOO_LONG Data too long.
/// 400 ENCRYPTION_DECLINED The secret chat was declined.
/// 400 FILE_EMTPY An empty file was provided.
/// 400 MD5_CHECKSUM_INVALID The MD5 checksums do not match.
/// 400 MSG_WAIT_FAILED A waiting call returned an error.
/// See <a href="https://corefork.telegram.org/method/messages.sendEncryptedFile" />
///</summary>
internal sealed class SendEncryptedFileHandler(ISecretChatService secretChats, IResponseCacheAppService responseCache) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendEncryptedFile, MyTelegram.Schema.Messages.ISentEncryptedMessage>
{
    protected override Task<MyTelegram.Schema.Messages.ISentEncryptedMessage> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendEncryptedFile obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        var rid = secretChats.AddMessage(chatId, input.UserId, obj.Data, hasFile: true);
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
