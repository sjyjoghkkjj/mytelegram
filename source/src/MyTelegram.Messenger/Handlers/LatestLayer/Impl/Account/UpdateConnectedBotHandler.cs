// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.updateConnectedBot" />
///</summary>
internal sealed class UpdateConnectedBotHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateConnectedBot, MyTelegram.Schema.IUpdates>,
    Account.IUpdateConnectedBotHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestUpdateConnectedBot obj)
    {
        throw new NotImplementedException();
    }
}
