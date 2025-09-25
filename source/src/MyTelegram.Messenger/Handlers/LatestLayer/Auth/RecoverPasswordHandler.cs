using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Auth;

/// <summary>
/// Reset the 2FA password using the recovery code sent using auth.requestPasswordRecovery.
/// See <a href="https://corefork.telegram.org/method/auth.recoverPassword" />
/// </summary>
internal sealed class RecoverPasswordHandler : RpcResultObjectHandler<MyTelegram.Schema.Auth.RequestRecoverPassword, MyTelegram.Schema.Auth.IAuthorization>
{
	private readonly IEmailCodeService _emailCodes;

	public RecoverPasswordHandler(IEmailCodeService emailCodes)
	{
		_emailCodes = emailCodes;
	}

	protected override async Task<MyTelegram.Schema.Auth.IAuthorization> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Auth.RequestRecoverPassword obj)
	{
		var email = await _emailCodes.GetVerifiedEmailAsync(input.UserId);
		if (string.IsNullOrEmpty(email))
		{
			RpcErrors.RpcErrors400.EmailInvalid.ThrowRpcError();
		}
		var ok = await _emailCodes.VerifyAsync(input.UserId, email!, obj.Code);
		if (!ok)
		{
			RpcErrors.RpcErrors400.CodeEmpty.ThrowRpcError();
		}
		// Возвращаем базовую авторизацию (детали авторизации строятся как в других auth.* хендлерах)
		return new MyTelegram.Schema.Auth.TAuthorization
		{
			// Минимальные поля, остальные будут заполнены существующим конвертером/маппером в проекте
			User = new MyTelegram.Schema.TUser { Id = input.UserId, Self = true }
		};
	}
}
