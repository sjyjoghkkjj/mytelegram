// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.updatePersonalChannel" />
///</summary>
internal sealed class UpdatePersonalChannelHandler(
    ICommandBus commandBus,
    IAccessHashHelper accessHashHelper,
    IChannelAppService channelAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdatePersonalChannel, IBool>,
        Account.IUpdatePersonalChannelHandler
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestUpdatePersonalChannel obj)
    {
        long? personalChannelId = null;
        switch (obj.Channel)
        {
            case TInputChannel inputChannel:
                await accessHashHelper.CheckAccessHashAsync(inputChannel);
                var channelReadModel =
                    await channelAppService.GetAsync(inputChannel.ChannelId);
                if (channelReadModel!.CreatorId != input.UserId)
                {
                    RpcErrors.RpcErrors400.ChatIdInvalid.ThrowRpcError();
                }

                personalChannelId = inputChannel.ChannelId;
                break;
        }

        var command =
            new UpdatePersonalChannelCommand(UserId.Create(input.UserId), input.ToRequestInfo(), personalChannelId);
        await commandBus.PublishAsync(command);

        return new TBoolTrue();
    }
}
