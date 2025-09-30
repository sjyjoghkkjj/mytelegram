namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Get private info associated to the password info (recovery email, telegram <a href="https://corefork.telegram.org/passport">passport</a> info &amp; so on)
/// <para>Possible errors</para>
/// Code Type Description
/// 400 PASSWORD_HASH_INVALID The provided password hash is invalid.
/// See <a href="https://corefork.telegram.org/method/account.getPasswordSettings" />
///</summary>
internal sealed class GetPasswordSettingsHandler(IPasswordService passwords) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetPasswordSettings, MyTelegram.Schema.Account.IPasswordSettings>
{
    protected override async Task<MyTelegram.Schema.Account.IPasswordSettings> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetPasswordSettings obj)
    {
        // Brute-force block applies here as well, since SRP must be valid
        if (await passwords is { } && await passwords.IsLoginBlockedAsync(input.UserId))
        {
            RpcErrors.RpcErrors420.FloodWaitX.ThrowRpcError(3600);
        }
        // Must verify password via SRP first
        var ok = await passwords.CheckPasswordAsync(input.UserId, obj.Password);
        if (!ok)
        {
            await passwords.RegisterFailedAttemptAsync(input.UserId, 5, 3600);
            RpcErrors.RpcErrors400.PasswordHashInvalid.ThrowRpcError();
        }

        await passwords.ResetFailedAttemptsAsync(input.UserId);
        var settings = await passwords.GetPasswordSettingsAsync(input.UserId);
        return settings;
    }
}
