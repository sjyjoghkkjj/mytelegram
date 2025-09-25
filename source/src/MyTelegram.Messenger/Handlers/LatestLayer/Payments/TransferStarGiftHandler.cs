namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

using MyTelegram.Messenger.Services;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/payments.transferStarGift" />
/// </summary>
internal sealed class TransferStarGiftHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestTransferStarGift, MyTelegram.Schema.IUpdates>
{
	private readonly ISavedStarGiftsService _savedService;
	private readonly IResponseCacheAppService _responseCache;

	public TransferStarGiftHandler(ISavedStarGiftsService savedService, IResponseCacheAppService responseCache)
	{
		_savedService = savedService;
		_responseCache = responseCache;
	}

	protected override Task<MyTelegram.Schema.Payments.IUpdates> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestTransferStarGift obj)
	{
		return HandleAsync(input, obj);
	}

	private async Task<MyTelegram.Schema.Payments.IUpdates> HandleAsync(IRequestInput input, MyTelegram.Schema.Payments.RequestTransferStarGift obj)
	{
		var toPeer = obj.Peer;
		if (toPeer is not MyTelegram.Schema.TInputPeerUser u)
		{
			RpcErrors.RpcErrors400.BadRequest("PEER_NOT_SUPPORTED").ThrowRpcError();
		}
		var toUserId = u.UserId;

		_savedService.TransferTo(input.UserId, toUserId, obj.Stargift, out var _);

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
