// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.Users;

///<summary>
/// See <a href="https://corefork.telegram.org/method/users.getRequirementsToContact" />
///</summary>
internal sealed class GetRequirementsToContactHandler : RpcResultObjectHandler<MyTelegram.Schema.Users.RequestGetRequirementsToContact, TVector<MyTelegram.Schema.IRequirementToContact>>,
    Users.IGetRequirementsToContactHandler
{
    protected override Task<TVector<MyTelegram.Schema.IRequirementToContact>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Users.RequestGetRequirementsToContact obj)
    {
        throw new NotImplementedException();
    }
}
