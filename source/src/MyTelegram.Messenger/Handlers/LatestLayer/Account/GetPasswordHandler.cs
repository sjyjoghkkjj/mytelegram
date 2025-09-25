using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

/// <summary>
/// Obtain configuration for two-factor authorization with password
/// See <a href="https://corefork.telegram.org/method/account.getPassword" />
/// </summary>
internal sealed class GetPasswordHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetPassword, MyTelegram.Schema.Account.IPassword>
{
	private readonly IPasswordService _passwords;

	public GetPasswordHandler(IPasswordService passwords)
	{
		_passwords = passwords;
	}

	protected override Task<MyTelegram.Schema.Account.IPassword> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Account.RequestGetPassword obj)
	{
		return _passwords.GetPasswordAsync(input.UserId).ContinueWith<MyTelegram.Schema.Account.IPassword>(t => t.Result);
	}
}
