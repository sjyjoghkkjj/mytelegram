using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/payments.updateStarGiftPrice" />
/// </summary>
internal sealed class UpdateStarGiftPriceHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestUpdateStarGiftPrice, MyTelegram.Schema.IUpdates>
{
	private readonly IResaleMarketService _market;

	public UpdateStarGiftPriceHandler(IResaleMarketService market)
	{
		_market = market;
	}

	protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestUpdateStarGiftPrice obj)
	{
		return HandleAsync(input, obj);
	}

	private async Task<MyTelegram.Schema.IUpdates> HandleAsync(IRequestInput input, MyTelegram.Schema.Payments.RequestUpdateStarGiftPrice obj)
	{
		await _market.UpsertListingAsync(input.UserId, obj.Stargift, obj.Price);
		return new TUpdates
		{
			Chats = [],
			Updates = [],
			Users = [],
			Date = CurrentDate
		};
	}
}
