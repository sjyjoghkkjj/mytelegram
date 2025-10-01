namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Verify an email to use as <a href="https://corefork.telegram.org/api/srp">2FA recovery method</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CODE_INVALID Code invalid.
/// 400 EMAIL_HASH_EXPIRED Email hash expired.
/// See <a href="https://corefork.telegram.org/method/account.confirmPasswordEmail" />
///</summary>
internal sealed class ConfirmPasswordEmailHandler(IEmailCodeService emailCodes, IPasswordService passwords) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestConfirmPasswordEmail, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestConfirmPasswordEmail obj)
    {
        const string scope = "confirm";
        if (await emailCodes.IsBlockedAsync(input.UserId, scope))
        {
            var left = await emailCodes.GetBlockSecondsAsync(input.UserId, scope);
            RpcErrors.RpcErrors420.FloodWaitX.ThrowRpcError(left);
        }

        var email = await passwords.GetUnconfirmedEmailAsync(input.UserId);
        if (string.IsNullOrEmpty(email))
        {
            RpcErrors.RpcErrors400.EmailHashExpired.ThrowRpcError();
        }
        var ok = await emailCodes.VerifyAsync(input.UserId, email!, obj.Code);
        if (!ok)
        {
            await emailCodes.RegisterFailedAttemptAsync(input.UserId, scope, 5, 3600);
            RpcErrors.RpcErrors400.CodeInvalid.ThrowRpcError();
        }
        await emailCodes.ResetFailedAttemptsAsync(input.UserId, scope);
        await passwords.SetVerifiedEmailAsync(input.UserId, email!);
        return new TBoolTrue();
    }
}
