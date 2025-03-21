// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.resolveBusinessChatLink" />
///</summary>
internal sealed class ResolveBusinessChatLinkHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestResolveBusinessChatLink, MyTelegram.Schema.Account.IResolvedBusinessChatLinks>,
    Account.IResolveBusinessChatLinkHandler
{
    protected override Task<MyTelegram.Schema.Account.IResolvedBusinessChatLinks> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestResolveBusinessChatLink obj)
    {
        throw new NotImplementedException();
    }
}
