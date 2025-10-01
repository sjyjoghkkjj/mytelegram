namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Marks message history within a secret chat as read.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 MSG_WAIT_FAILED A waiting call returned an error.
/// See <a href="https://corefork.telegram.org/method/messages.readEncryptedHistory" />
///</summary>
internal sealed class ReadEncryptedHistoryHandler(ISecretChatService secretChats) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestReadEncryptedHistory, IBool>
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestReadEncryptedHistory obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        secretChats.MarkRead(chatId, input.UserId);
        return Task.FromResult<IBool>(new TBoolTrue());
    }
}
