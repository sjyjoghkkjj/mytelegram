// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.updateBusinessIntro" />
///</summary>
internal sealed class UpdateBusinessIntroHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateBusinessIntro, IBool>,
    Account.IUpdateBusinessIntroHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestUpdateBusinessIntro obj)
    {
        throw new NotImplementedException();
    }
}
