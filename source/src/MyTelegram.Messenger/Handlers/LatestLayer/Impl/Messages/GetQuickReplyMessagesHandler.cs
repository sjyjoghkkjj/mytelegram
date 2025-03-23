// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getQuickReplyMessages" />
///</summary>
internal sealed class GetQuickReplyMessagesHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetQuickReplyMessages, MyTelegram.Schema.Messages.IMessages>,
    Messages.IGetQuickReplyMessagesHandler
{
    protected override Task<MyTelegram.Schema.Messages.IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetQuickReplyMessages obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.IMessages>(new TMessages
        {
            Chats = new(),
            Messages = new(),
            Users = new()
        });
    }
}
