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
internal sealed class SendEncryptedFileHandler(ISecretChatService secretChats, IResponseCacheAppService responseCache, IMediaHelper mediaHelper, ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendEncryptedFile, MyTelegram.Schema.Messages.ISentEncryptedMessage>
{
    protected override Task<MyTelegram.Schema.Messages.ISentEncryptedMessage> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendEncryptedFile obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        var rid = secretChats.AddMessage(chatId, input.UserId, obj.Data, hasFile: true);
        var state = secretChats.Get(chatId)!;
        var toUserId = state.AdminId == input.UserId ? state.ParticipantId : state.AdminId;
        // Save encrypted file via media service
        var encFileTask = mediaHelper.SaveEncryptedFileAsync(input.ReqMsgId, obj.File);
        encFileTask.Wait();
        var encFile = encFileTask.Result;
        // Build TL update for persistent push
        var updBytes = new TUpdateNewEncryptedMessage
        {
            Message = new TEncryptedMessage
            {
                ChatId = chatId,
                Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RandomId = rid,
                Bytes = obj.Data,
                File = encFile
            },
            Qts = 0
        }.ToBytes();
        commandBus.PublishAsync(new MyTelegram.Domain.Commands.PushUpdates.CreateEncryptedPushUpdatesCommand(new MyTelegram.Domain.Aggregates.PushUpdates.PushUpdatesId(toUserId), toUserId, updBytes, 0, input.PermAuthKeyId!.Value)).GetAwaiter().GetResult();
        commandBus.PublishAsync(new MyTelegram.Domain.Commands.Pts.IncrementQtsCommand(new MyTelegram.Domain.Aggregates.Pts.PtsId(toUserId), input.ToRequestInfo(), rid.ToString())).GetAwaiter().GetResult();
        // Also queue local update for immediate response
        foreach (var u in secretChats.BuildPendingUpdates(toUserId))
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
