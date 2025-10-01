namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Confirms creation of a secret chat
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// 400 ENCRYPTION_ALREADY_ACCEPTED Secret chat already accepted.
/// 400 ENCRYPTION_ALREADY_DECLINED The secret chat was already declined.
/// See <a href="https://corefork.telegram.org/method/messages.acceptEncryption" />
///</summary>
internal sealed class AcceptEncryptionHandler(ISecretChatService secretChats) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestAcceptEncryption, MyTelegram.Schema.IEncryptedChat>
{
    protected override Task<MyTelegram.Schema.IEncryptedChat> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestAcceptEncryption obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        var gB = obj.GB.ToArray();
        var state = secretChats.Accept(chatId, input.UserId, gB, obj.KeyFingerprint);
        return Task.FromResult<MyTelegram.Schema.IEncryptedChat>(new TEncryptedChat
        {
            Id = state.ChatId,
            AccessHash = state.AccessHash,
            Date = state.Date,
            AdminId = state.AdminId,
            ParticipantId = state.ParticipantId,
            GAOrB = gB,
            KeyFingerprint = state.KeyFingerprint ?? 0
        });
    }
}
