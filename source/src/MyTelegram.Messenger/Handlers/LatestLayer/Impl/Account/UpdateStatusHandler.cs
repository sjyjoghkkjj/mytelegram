// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Updates online user status.
/// <para>Possible errors</para>
/// Code Type Description
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// See <a href="https://corefork.telegram.org/method/account.updateStatus" />
///</summary>
internal sealed class UpdateStatusHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdateStatus, IBool>,
    Account.IUpdateStatusHandler
{
    private readonly IUserStatusCacheAppService _userStatusAppService;

    public UpdateStatusHandler(IUserStatusCacheAppService userStatusAppService)
    {
        _userStatusAppService = userStatusAppService;
    }

    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        RequestUpdateStatus obj)
    {
        _userStatusAppService.UpdateStatus(input.UserId, !obj.Offline);

        return Task.FromResult<IBool>(new TBoolTrue());
    }
}
