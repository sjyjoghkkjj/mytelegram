// ReSharper disable All

using EventFlow.Aggregates.ExecutionResults;
using MyTelegram.Domain.Aggregates.Device;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Auth;

///<summary>
/// Terminates all user's authorized sessions except for the current one.After calling this method it is necessary to reregister the current device using the method <a href="https://corefork.telegram.org/method/account.registerDevice">account.registerDevice</a>
/// See <a href="https://corefork.telegram.org/method/auth.resetAuthorizations" />
///</summary>
internal sealed class ResetAuthorizationsHandler(
    IQueryProcessor queryProcessor,
    IObjectMessageSender messageSender,
    IEventBus eventBus)
    : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestResetAuthorizations, IBool>,
        Auth.IResetAuthorizationsHandler
{
    private readonly IObjectMessageSender _messageSender = messageSender;

    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        RequestResetAuthorizations obj)
    {
        var deviceList = await queryProcessor
            .ProcessAsync(new GetDeviceByUserIdQuery(input.UserId));
        //foreach (var deviceReadModel in deviceList)
        //{
            
        //}

        //foreach (var deviceReadModel in deviceList)
        //{
        //    if (deviceReadModel.PermAuthKeyId == input.PermAuthKeyId)
        //    {
        //        continue;
        //    }

        //    await _eventBus.PublishAsync(new UnRegisterAuthKeyEvent(deviceReadModel.PermAuthKeyId));
        //}

        //var updatesTooLong = new TUpdatesTooLong();

        //await _messageSender.PushMessageToPeerAsync(new Peer(PeerType.User, input.UserId),
        //    updatesTooLong,
        //    input.AuthKeyId);
        var revokedAuthKeyIdList = deviceList.Where(p => p.PermAuthKeyId != input.PermAuthKeyId).Select(p => p.PermAuthKeyId).ToList();
        await eventBus.PublishAsync(new SessionRevokedEvent(input.PermAuthKeyId, revokedAuthKeyIdList));

        return new TBoolTrue();
    }
}
