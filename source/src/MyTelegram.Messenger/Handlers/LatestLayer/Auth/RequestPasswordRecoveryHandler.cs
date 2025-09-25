using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Auth;

/// <summary>
/// Request recovery code of a 2FA password, only for accounts with a recovery email configured.
/// See <a href="https://corefork.telegram.org/method/auth.requestPasswordRecovery" />
/// </summary>
internal sealed class RequestPasswordRecoveryHandler : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestRequestPasswordRecovery, MyTelegram.Schema.Auth.IPasswordRecovery>
{
	private readonly IEmailCodeService _emailCodes;
	private readonly IOptionsMonitor<MyTelegramMessengerServerOptions> _options;

	public RequestPasswordRecoveryHandler(IEmailCodeService emailCodes, IOptionsMonitor<MyTelegramMessengerServerOptions> options)
	{
		_emailCodes = emailCodes;
		_options = options;
	}

	protected override async Task<MyTelegram.Schema.Auth.IPasswordRecovery> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Auth.RequestRequestPasswordRecovery obj)
	{
		var email = await _emailCodes.GetVerifiedEmailAsync(input.UserId);
		if (string.IsNullOrEmpty(email))
		{
			RpcErrors.RpcErrors400.EmailInvalid.ThrowRpcError();
		}
		var ttl = TimeSpan.FromSeconds(_options.CurrentValue.VerificationCodeExpirationSeconds);
		await _emailCodes.CreateAsync(input.UserId, email!, ttl);
		var (name, domain) = Obfuscate(email!);
		return new MyTelegram.Schema.Auth.TPasswordRecovery { EmailPattern = $"{name}@{domain}" };
	}

	private static (string name, string domain) Obfuscate(string email)
	{
		var parts = email.Split('@');
		if (parts.Length != 2) return ("***", "***");
		string mask(string s) => s.Length <= 2 ? "**" : s[0] + new string('*', s.Length - 2) + s[^1];
		return (mask(parts[0]), mask(parts[1]));
	}
}
