using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/payments.getStarGiftUpgradePreview" />
/// </summary>
internal sealed class GetStarGiftUpgradePreviewHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestGetStarGiftUpgradePreview, MyTelegram.Schema.Payments.IStarGiftUpgradePreview>
{
	private readonly ISavedStarGiftsService _savedService;

	public GetStarGiftUpgradePreviewHandler(ISavedStarGiftsService savedService)
	{
		_savedService = savedService;
	}

	protected override Task<MyTelegram.Schema.Payments.IStarGiftUpgradePreview> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestGetStarGiftUpgradePreview obj)
	{
		var preview = _savedService.BuildUpgradePreview(input.UserId, obj.GiftId);
		return Task.FromResult<MyTelegram.Schema.Payments.IStarGiftUpgradePreview>(preview);
	}
}
