// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.viewSponsoredMessage" />
///</summary>
internal sealed class ViewSponsoredMessageHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestViewSponsoredMessage, IBool>,
    Messages.IViewSponsoredMessageHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestViewSponsoredMessage obj)
    {
        throw new NotImplementedException();
    }
}
