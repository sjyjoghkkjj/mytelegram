// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getSponsoredMessages" />
///</summary>
internal sealed class GetSponsoredMessagesHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetSponsoredMessages, MyTelegram.Schema.Messages.ISponsoredMessages>,
    Messages.IGetSponsoredMessagesHandler
{
    protected override Task<MyTelegram.Schema.Messages.ISponsoredMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetSponsoredMessages obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.ISponsoredMessages>(new TSponsoredMessagesEmpty());
    }
}
