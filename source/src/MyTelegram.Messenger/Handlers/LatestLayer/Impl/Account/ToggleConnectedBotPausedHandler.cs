// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.toggleConnectedBotPaused" />
///</summary>
internal sealed class ToggleConnectedBotPausedHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestToggleConnectedBotPaused, IBool>,
    Account.IToggleConnectedBotPausedHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestToggleConnectedBotPaused obj)
    {
        throw new NotImplementedException();
    }
}
