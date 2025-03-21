// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Contacts;

///<summary>
/// See <a href="https://corefork.telegram.org/method/contacts.getBirthdays" />
///</summary>
internal sealed class GetBirthdaysHandler : RpcResultObjectHandler<MyTelegram.Schema.Contacts.RequestGetBirthdays, MyTelegram.Schema.Contacts.IContactBirthdays>,
    Contacts.IGetBirthdaysHandler
{
    protected override Task<MyTelegram.Schema.Contacts.IContactBirthdays> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Contacts.RequestGetBirthdays obj)
    {
        return Task.FromResult<MyTelegram.Schema.Contacts.IContactBirthdays>(new TContactBirthdays
        {
            Contacts = new(),
            Users = new()
        });
    }
}
