using MyTelegram.Messenger.Handlers.LatestLayer.Interfaces.Users;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Users;

///<summary>
/// See <a href="https://corefork.telegram.org/method/users.getRequirementsToContact" />
///</summary>
internal sealed class GetRequirementsToContactHandler : RpcResultObjectHandler<MyTelegram.Schema.Users.RequestGetRequirementsToContact, TVector<MyTelegram.Schema.IRequirementToContact>>,
    IGetRequirementsToContactHandler
{
    protected override Task<TVector<MyTelegram.Schema.IRequirementToContact>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Users.RequestGetRequirementsToContact obj)
    {
        return Task.FromResult<TVector<MyTelegram.Schema.IRequirementToContact>>([]);
    }
}
