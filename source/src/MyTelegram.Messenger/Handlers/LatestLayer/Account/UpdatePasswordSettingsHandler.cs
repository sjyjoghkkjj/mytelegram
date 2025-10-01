using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

/// <summary>
/// Set a new 2FA password
/// See <a href="https://corefork.telegram.org/method/account.updatePasswordSettings" />
/// </summary>
internal sealed class UpdatePasswordSettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestUpdatePasswordSettings, IBool>
{
	private readonly IPasswordService _passwords;

	public UpdatePasswordSettingsHandler(IPasswordService passwords)
	{
		_passwords = passwords;
	}

	protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Account.RequestUpdatePasswordSettings obj)
	{
		if (obj.NewSettings is not MyTelegram.Schema.Account.TPasswordInputSettings s)
		{
			RpcErrors.RpcErrors400.NewSettingsInvalid.ThrowRpcError();
		}
        // if a password already exists, require the current password SRP verification
        var current = await _passwords.GetPasswordAsync(input.UserId);
        if (current.HasPassword)
        {
            if (await _passwords.IsLoginBlockedAsync(input.UserId))
            {
                RpcErrors.RpcErrors420.FloodWaitX.ThrowRpcError(3600);
            }
            var ok = await _passwords.CheckPasswordAsync(input.UserId, obj.Password);
            if (!ok)
            {
                await _passwords.RegisterFailedAttemptAsync(input.UserId, 5, 3600);
                RpcErrors.RpcErrors400.PasswordHashInvalid.ThrowRpcError();
            }
            await _passwords.ResetFailedAttemptsAsync(input.UserId);
        }
		await _passwords.SetPasswordAsync(input.UserId, s);
		return new TBoolTrue();
	}
}
