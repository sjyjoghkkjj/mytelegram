namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Send typing event by the current user to a secret chat.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.setEncryptedTyping" />
///</summary>
internal sealed class SetEncryptedTypingHandler(IResponseCacheAppService responseCache) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSetEncryptedTyping, IBool>
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSetEncryptedTyping obj)
    {
        var chatId = (obj.Peer as TInputEncryptedChat)!.ChatId;
        responseCache.AddToCache(input.ReqMsgId, new TUpdateEncryptedChatTyping { ChatId = chatId });
        return Task.FromResult<IBool>(new TBoolTrue());
    }
}
