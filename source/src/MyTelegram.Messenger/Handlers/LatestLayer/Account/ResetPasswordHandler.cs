namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Initiate a 2FA password reset: can only be used if the user is already logged-in, <a href="https://corefork.telegram.org/api/srp#password-reset">see here for more info »</a>
/// See <a href="https://corefork.telegram.org/method/account.resetPassword" />
///</summary>
internal sealed class ResetPasswordHandler(IPasswordService passwords, IOptionsMonitor<MyTelegramMessengerServerOptions> options) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestResetPassword, MyTelegram.Schema.Account.IResetPasswordResult>
{
    protected override async Task<MyTelegram.Schema.Account.IResetPasswordResult> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestResetPassword obj)
    {
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var retry = await passwords.GetResetRetryDateAsync(input.UserId);
        if (retry.HasValue && now < retry.Value)
        {
            return new MyTelegram.Schema.Account.TResetPasswordFailedWait { RetryDate = retry.Value };
        }

        var pending = await passwords.GetPendingResetDateAsync(input.UserId);
        if (!pending.HasValue || now >= pending.Value)
        {
            // schedule new pending reset after wait window
            var waitSeconds = Math.Clamp(options.CurrentValue.VerificationCodeExpirationSeconds * 12, 300, 86400); // heuristic window
            var until = now + waitSeconds;
            await passwords.SetPendingResetDateAsync(input.UserId, until);
            return new MyTelegram.Schema.Account.TResetPasswordRequestedWait { UntilDate = until };
        }

        // finalize if already pending and window elapsed
        await passwords.ClearPasswordAsync(input.UserId);
        await passwords.SetPendingResetDateAsync(input.UserId, null);
        return new MyTelegram.Schema.Account.TResetPasswordOk();
    }
}
