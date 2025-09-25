using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Payments;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/payments.toggleChatStarGiftNotifications" />
/// </summary>
internal sealed class ToggleChatStarGiftNotificationsHandler : RpcResultObjectHandler<MyTelegram.Schema.Payments.RequestToggleChatStarGiftNotifications, IBool>
{
	private readonly ISavedStarGiftsService _savedService;

	public ToggleChatStarGiftNotificationsHandler(ISavedStarGiftsService savedService)
	{
		_savedService = savedService;
	}

	protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Payments.RequestToggleChatStarGiftNotifications obj)
	{
		var peer = obj.Peer;
		long chatId = 0;
		if (peer is MyTelegram.Schema.TInputPeerChat c) chatId = c.ChatId;
		else if (peer is MyTelegram.Schema.TInputPeerChannel ch) chatId = ch.ChannelId;
		else if (peer is MyTelegram.Schema.TInputPeerUser u) chatId = u.UserId;
		else RpcErrors.RpcErrors400.BadRequest("PEER_NOT_SUPPORTED").ThrowRpcError();

		await _savedService.ToggleChatNotificationsAsync(input.UserId, chatId, obj.Enabled);
		return new TBoolTrue();
	}
}
