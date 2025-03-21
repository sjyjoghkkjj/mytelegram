// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.updateBusinessLocation" />
///</summary>
internal sealed class UpdateBusinessLocationHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateBusinessLocation, IBool>,
    Account.IUpdateBusinessLocationHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestUpdateBusinessLocation obj)
    {
        throw new NotImplementedException();
    }
}
