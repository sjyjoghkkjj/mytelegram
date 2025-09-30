using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Auth;

/// <summary>
/// Try logging to an account protected by a 2FA password (SRP)
/// See <a href="https://corefork.telegram.org/method/auth.checkPassword" />
/// </summary>
internal sealed class CheckPasswordHandler : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestCheckPassword, MyTelegram.Schema.Auth.IAuthorization>
{
	private readonly IPasswordService _passwords;

	public CheckPasswordHandler(IPasswordService passwords)
	{
		_passwords = passwords;
	}

    protected override async Task<MyTelegram.Schema.Auth.IAuthorization> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Auth.RequestCheckPassword obj)
    {
        // Check brute-force block
        if (await _passwords.IsLoginBlockedAsync(input.UserId))
        {
            // 3600s block window
            RpcErrors.RpcErrors420.FloodWaitX.ThrowRpcError(3600);
        }

        var ok = await _passwords.CheckPasswordAsync(input.UserId, obj.Password);
        if (!ok)
        {
            await _passwords.RegisterFailedAttemptAsync(input.UserId, maxAttempts: 5, blockSeconds: 3600);
            RpcErrors.RpcErrors400.PasswordHashInvalid.ThrowRpcError();
        }

        await _passwords.ResetFailedAttemptsAsync(input.UserId);
        return new MyTelegram.Schema.Auth.TAuthorization { User = new MyTelegram.Schema.TUser { Id = input.UserId, Self = true } };
    }
}
