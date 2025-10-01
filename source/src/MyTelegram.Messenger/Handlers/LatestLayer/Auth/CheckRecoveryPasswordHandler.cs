namespace MyTelegram.Messenger.Handlers.LatestLayer.Auth;

///<summary>
/// Check if the <a href="https://corefork.telegram.org/api/srp">2FA recovery code</a> sent using <a href="https://corefork.telegram.org/method/auth.requestPasswordRecovery">auth.requestPasswordRecovery</a> is valid, before passing it to <a href="https://corefork.telegram.org/method/auth.recoverPassword">auth.recoverPassword</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PASSWORD_RECOVERY_EXPIRED The recovery code has expired.
/// See <a href="https://corefork.telegram.org/method/auth.checkRecoveryPassword" />
///</summary>
internal sealed class CheckRecoveryPasswordHandler(IEmailCodeService emailCodes) : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestCheckRecoveryPassword, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Auth.RequestCheckRecoveryPassword obj)
    {
        const string scope = "recover";
        if (await emailCodes.IsBlockedAsync(input.UserId, scope))
        {
            var left = await emailCodes.GetBlockSecondsAsync(input.UserId, scope);
            RpcErrors.RpcErrors420.FloodWaitX.ThrowRpcError(left);
        }

        var email = await emailCodes.GetVerifiedEmailAsync(input.UserId);
        if (string.IsNullOrEmpty(email))
        {
            RpcErrors.RpcErrors400.PasswordRecoveryExpired.ThrowRpcError();
        }
        var ok = await emailCodes.VerifyAsync(input.UserId, email!, obj.Code);
        if (!ok)
        {
            await emailCodes.RegisterFailedAttemptAsync(input.UserId, scope, 5, 3600);
            RpcErrors.RpcErrors400.PasswordRecoveryExpired.ThrowRpcError();
        }
        await emailCodes.ResetFailedAttemptsAsync(input.UserId, scope);
        return new TBoolTrue();
    }
}
