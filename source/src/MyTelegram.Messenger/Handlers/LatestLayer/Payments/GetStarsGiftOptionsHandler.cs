namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

/// <summary>
/// Obtain a list of Telegram Stars gift options as starsGiftOption constructors.
/// See <a href="https://corefork.telegram.org/method/payments.getStarsGiftOptions" />
/// </summary>
internal sealed class GetStarsGiftOptionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarsGiftOptions, TVector<MyTelegram.Schema.IStarsGiftOption>>
{
	protected override Task<TVector<MyTelegram.Schema.IStarsGiftOption>> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestGetStarsGiftOptions obj)
	{
		// Пример опций: 100, 500, 1000 звёзд (покупка/подарок)
		var options = new TVector<MyTelegram.Schema.IStarsGiftOption>(new MyTelegram.Schema.IStarsGiftOption[]
		{
			new MyTelegram.Schema.TStarsGiftOption { Stars = 100, TopupStars = 100, Price = 100 },
			new MyTelegram.Schema.TStarsGiftOption { Stars = 500, TopupStars = 500, Price = 480 },
			new MyTelegram.Schema.TStarsGiftOption { Stars = 1000, TopupStars = 1000, Price = 940 }
		});
		return Task.FromResult(options);
	}
}
