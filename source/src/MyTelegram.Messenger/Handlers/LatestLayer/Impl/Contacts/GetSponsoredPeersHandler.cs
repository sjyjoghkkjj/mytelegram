// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.Contacts;

///<summary>
/// See <a href="https://corefork.telegram.org/method/contacts.getSponsoredPeers" />
///</summary>
internal sealed class GetSponsoredPeersHandler : RpcResultObjectHandler<MyTelegram.Schema.Contacts.RequestGetSponsoredPeers, MyTelegram.Schema.Contacts.ISponsoredPeers>,
    Contacts.IGetSponsoredPeersHandler
{
    protected override Task<MyTelegram.Schema.Contacts.ISponsoredPeers> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Contacts.RequestGetSponsoredPeers obj)
    {
        throw new NotImplementedException();
    }
}
