namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Resend the code to verify an email to use as <a href="https://corefork.telegram.org/api/srp">2FA recovery method</a>.
/// See <a href="https://corefork.telegram.org/method/account.resendPasswordEmail" />
///</summary>
internal sealed class ResendPasswordEmailHandler(IEmailCodeService emailCodes, IPasswordService passwords, IOptionsMonitor<MyTelegramMessengerServerOptions> options) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestResendPasswordEmail, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestResendPasswordEmail obj)
    {
        var email = await passwords.GetUnconfirmedEmailAsync(input.UserId) ?? await passwords.GetVerifiedEmailAsync(input.UserId);
        if (string.IsNullOrEmpty(email))
        {
            RpcErrors.RpcErrors400.EmailInvalid.ThrowRpcError();
        }
        var ttl = TimeSpan.FromSeconds(options.CurrentValue.VerificationCodeExpirationSeconds);
        await emailCodes.CreateAsync(input.UserId, email!, ttl);
        return new TBoolTrue();
    }
}
