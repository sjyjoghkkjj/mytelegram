using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

/// <summary>
/// Send an email verification code.
/// See <a href="https://corefork.telegram.org/method/account.sendVerifyEmailCode" />
/// </summary>
internal sealed class SendVerifyEmailCodeHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestSendVerifyEmailCode, MyTelegram.Schema.Account.ISentEmailCode>
{
	private readonly IEmailCodeService _emailCodes;
	private readonly IOptionsMonitor<MyTelegramMessengerServerOptions> _options;

	public SendVerifyEmailCodeHandler(IEmailCodeService emailCodes, IOptionsMonitor<MyTelegramMessengerServerOptions> options)
	{
		_emailCodes = emailCodes;
		_options = options;
	}

	protected override async Task<MyTelegram.Schema.Account.ISentEmailCode> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Account.RequestSendVerifyEmailCode obj)
	{
		var ttl = TimeSpan.FromSeconds(_options.CurrentValue.VerificationCodeExpirationSeconds);
		var (_, expire) = await _emailCodes.CreateAsync(input.UserId, obj.Email, ttl);
		return new MyTelegram.Schema.Account.TSentEmailCode
		{
			Email = obj.Email,
			Length = _options.CurrentValue.VerificationCodeLength,
			Expire = expire
		};
	}
}
