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
		await _passwords.SetPasswordAsync(input.UserId, s);
		return new TBoolTrue();
	}
}
