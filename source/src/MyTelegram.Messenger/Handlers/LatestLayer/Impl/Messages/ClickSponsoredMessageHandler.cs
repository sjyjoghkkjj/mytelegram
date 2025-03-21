// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.clickSponsoredMessage" />
///</summary>
internal sealed class ClickSponsoredMessageHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestClickSponsoredMessage, IBool>,
    Messages.IClickSponsoredMessageHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestClickSponsoredMessage obj)
    {
        throw new NotImplementedException();
    }
}
