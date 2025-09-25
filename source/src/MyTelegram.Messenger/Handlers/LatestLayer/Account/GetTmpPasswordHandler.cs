using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

/// <summary>
/// Get temporary payment password
/// See <a href="https://corefork.telegram.org/method/account.getTmpPassword" />
/// </summary>
internal sealed class GetTmpPasswordHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetTmpPassword, MyTelegram.Schema.Account.ITmpPassword>
{
	private readonly IPasswordService _passwords;

	public GetTmpPasswordHandler(IPasswordService passwords)
	{
		_passwords = passwords;
	}

	protected override Task<MyTelegram.Schema.Account.ITmpPassword> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Account.RequestGetTmpPassword obj)
	{
		return _passwords.CreateTmpPasswordAsync(input.UserId, obj.Purpose, obj.Period).ContinueWith<MyTelegram.Schema.Account.ITmpPassword>(t => t.Result);
	}
}
