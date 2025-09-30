namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Cancel the code that was sent to verify an email to use as <a href="https://corefork.telegram.org/api/srp">2FA recovery method</a>.
/// See <a href="https://corefork.telegram.org/method/account.cancelPasswordEmail" />
///</summary>
internal sealed class CancelPasswordEmailHandler(IPasswordService passwords) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestCancelPasswordEmail, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestCancelPasswordEmail obj)
    {
        await passwords.CancelUnconfirmedEmailAsync(input.UserId);
        return new TBoolTrue();
    }
}
