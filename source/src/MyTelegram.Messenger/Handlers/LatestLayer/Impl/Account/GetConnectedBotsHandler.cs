// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.getConnectedBots" />
///</summary>
internal sealed class GetConnectedBotsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetConnectedBots, MyTelegram.Schema.Account.IConnectedBots>,
    Account.IGetConnectedBotsHandler
{
    protected override Task<MyTelegram.Schema.Account.IConnectedBots> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetConnectedBots obj)
    {
        return Task.FromResult<MyTelegram.Schema.Account.IConnectedBots>(new TConnectedBots
        {
            ConnectedBots = new(),
            Users = new()
        });
    }
}
