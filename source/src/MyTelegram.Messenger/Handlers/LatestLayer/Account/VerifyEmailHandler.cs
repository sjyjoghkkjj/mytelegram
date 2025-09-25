using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

/// <summary>
/// Verify an email address.
/// See <a href="https://corefork.telegram.org/method/account.verifyEmail" />
/// </summary>
internal sealed class VerifyEmailHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestVerifyEmail, MyTelegram.Schema.Account.IEmailVerified>
{
	private readonly IEmailCodeService _emailCodes;

	public VerifyEmailHandler(IEmailCodeService emailCodes)
	{
		_emailCodes = emailCodes;
	}

	protected override async Task<MyTelegram.Schema.Account.IEmailVerified> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Account.RequestVerifyEmail obj)
	{
		var ok = await _emailCodes.VerifyAsync(input.UserId, obj.Email, obj.Code);
		if (!ok)
		{
			RpcErrors.RpcErrors400.EmailVerifyExpired.ThrowRpcError();
		}
		await _emailCodes.SetVerifiedEmailAsync(input.UserId, obj.Email);
		await _emailCodes.SetEmailLoginEnabledAsync(input.UserId, true);
		return new MyTelegram.Schema.Account.TEmailVerifiedLogin { Email = obj.Email };
	}
}
