using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/payments.upgradeStarGift" />
/// </summary>
internal sealed class UpgradeStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestUpgradeStarGift, MyTelegram.Schema.IUpdates>
{
	private readonly ISavedStarGiftsService _savedService;
	private readonly IResponseCacheAppService _responseCache;

	public UpgradeStarGiftHandler(ISavedStarGiftsService savedService, IResponseCacheAppService responseCache)
	{
		_savedService = savedService;
		_responseCache = responseCache;
	}

	protected override Task<MyTelegram.Schema.Payments.IUpdates> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestUpgradeStarGift obj)
	{
		return HandleAsync(input, obj);
	}

	private async Task<MyTelegram.Schema.Payments.IUpdates> HandleAsync(IRequestInput input, MyTelegram.Schema.Payments.RequestUpgradeStarGift obj)
	{
		if (!_savedService.TryGetSaved(input.UserId, obj.Stargift, out var saved))
		{
			RpcErrors.RpcErrors400.BadRequest("STARGIFT_NOT_FOUND").ThrowRpcError();
		}

		var price = saved.UpgradeStars ?? (saved.Gift as MyTelegram.Schema.TStarGift)?.UpgradeStars ?? 0;
		if (price <= 0)
		{
			RpcErrors.RpcErrors400.BadRequest("STARGIFT_UPGRADE_NOT_AVAILABLE").ThrowRpcError();
		}

        _savedService.DeductStars(input.UserId, price);
        // Применяем апгрейд
		saved.PinnedTop = false;
		saved.Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Зарегистрируем апгрейд, чтобы задать КД на передачу/реселл при первом апгрейде
        if (saved.Gift is MyTelegram.Schema.TStarGift g)
        {
            (_savedService as SavedStarGiftsService)?.RegisterUpgrade(input.UserId, g.Id);
        }

		var balance = await _savedService.GetStarsBalanceAsync(input.UserId);
		var update = new TUpdateStarsBalance { Stars = balance };
		await _responseCache.PushToQueueAsync(input.UserId, update);

		return new MyTelegram.Schema.TUpdates
		{
			Updates = new TVector<MyTelegram.Schema.IUpdate>(),
			Users = new TVector<MyTelegram.Schema.IUser>(),
			Chats = new TVector<MyTelegram.Schema.IChat>(),
			Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			Seq = 0
		};
	}
}
