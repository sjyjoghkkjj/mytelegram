// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

/// <summary>
///     Reset all active web <a href="https://corefork.telegram.org/widgets/login">telegram login</a> sessions
///     See <a href="https://corefork.telegram.org/method/account.resetWebAuthorizations" />
/// </summary>
internal sealed class ResetWebAuthorizationsHandler(
    IQueryProcessor queryProcessor,
    IEventBus eventBus) : RpcResultObjectHandler<Schema.Account.RequestResetWebAuthorizations, IBool>,
    Account.IResetWebAuthorizationsHandler
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        Schema.Account.RequestResetWebAuthorizations obj)
    {
        var deviceList = await queryProcessor
            .ProcessAsync(new GetDeviceByUserIdQuery(input.UserId));
        var revokedAuthKeyIdList = deviceList
            .Where(p => p.PermAuthKeyId != input.PermAuthKeyId && p.AppVersion.Contains("web"))
            .Select(p => p.PermAuthKeyId).ToList();
        await eventBus.PublishAsync(new SessionRevokedEvent(input.PermAuthKeyId, revokedAuthKeyIdList));
        return new TBoolTrue();
    }
}