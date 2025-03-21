// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.updateBirthday" />
///</summary>
internal sealed class UpdateBirthdayHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateBirthday, IBool>,
    Account.IUpdateBirthdayHandler
{
    private readonly ICommandBus _commandBus;

    public UpdateBirthdayHandler(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestUpdateBirthday obj)
    {
        Birthday? birthday = null;
        if (obj.Birthday != null)
        {
            birthday = new Birthday(obj.Birthday.Day, obj.Birthday.Month, obj.Birthday.Year);
        }

        var command = new UpdateBirthdayCommand(UserId.Create(input.UserId), birthday);
        await _commandBus.PublishAsync(command);

        return new TBoolTrue();
    }
}
