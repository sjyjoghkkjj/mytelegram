// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Deletes a device by its token, stops sending PUSH-notifications to it.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 TOKEN_INVALID The provided token is invalid.
/// See <a href="https://corefork.telegram.org/method/account.unregisterDevice" />
///</summary>
internal sealed class UnregisterDeviceHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUnregisterDevice, IBool>,
    Account.IUnregisterDeviceHandler
{
    private readonly ICommandBus _commandBus;

    public UnregisterDeviceHandler(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        RequestUnregisterDevice obj)
    {
        var command = new UnRegisterDeviceCommand(PushDeviceId.Create(obj.Token),
            input.ToRequestInfo(),
            obj.TokenType,
            obj.Token,
            obj.OtherUids.ToList());
        await _commandBus.PublishAsync(command, CancellationToken.None);

        return new TBoolTrue();
    }
}
