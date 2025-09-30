namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Abort a pending 2FA password reset, <a href="https://corefork.telegram.org/api/srp#password-reset">see here for more info »</a>
/// See <a href="https://corefork.telegram.org/method/account.declinePasswordReset" />
///</summary>
internal sealed class DeclinePasswordResetHandler(IPasswordService passwords) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestDeclinePasswordReset, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestDeclinePasswordReset obj)
    {
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // cancel pending reset and set a retry cooldown
        await passwords.SetPendingResetDateAsync(input.UserId, null);
        // set retry window to prevent immediate re-request
        await passwords.SetResetRetryDateAsync(input.UserId, now + 3600);
        return new TBoolTrue();
    }
}
