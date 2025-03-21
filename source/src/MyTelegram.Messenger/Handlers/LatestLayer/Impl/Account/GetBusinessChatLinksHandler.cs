// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.getBusinessChatLinks" />
///</summary>
internal sealed class GetBusinessChatLinksHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetBusinessChatLinks, MyTelegram.Schema.Account.IBusinessChatLinks>,
    Account.IGetBusinessChatLinksHandler
{
    protected override Task<MyTelegram.Schema.Account.IBusinessChatLinks> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetBusinessChatLinks obj)
    {
        return Task.FromResult<MyTelegram.Schema.Account.IBusinessChatLinks>(new TBusinessChatLinks
        {
            Chats = new(),
            Links = new(),
            Users = new()
        });
    }
}
