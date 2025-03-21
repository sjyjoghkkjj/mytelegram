// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Contacts;

///<summary>
/// See <a href="https://corefork.telegram.org/method/contacts.setBlocked" />
///</summary>
internal sealed class SetBlockedHandler : RpcResultObjectHandler<MyTelegram.Schema.Contacts.RequestSetBlocked, IBool>,
    Contacts.ISetBlockedHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Contacts.RequestSetBlocked obj)
    {
        throw new NotImplementedException();
    }
}
