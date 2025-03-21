// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.toggleSponsoredMessages" />
///</summary>
internal sealed class ToggleSponsoredMessagesHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestToggleSponsoredMessages, IBool>,
    Account.IToggleSponsoredMessagesHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestToggleSponsoredMessages obj)
    {
        return Task.FromResult<IBool>(new TBoolTrue());
    }
}
