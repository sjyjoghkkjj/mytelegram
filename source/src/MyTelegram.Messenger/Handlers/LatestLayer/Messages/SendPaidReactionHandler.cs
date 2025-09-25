using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// See <a href="https://corefork.telegram.org/method/messages.sendPaidReaction" />
/// </summary>
internal sealed class SendPaidReactionHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendPaidReaction, MyTelegram.Schema.IUpdates>
{
	private readonly IPaidReactionsService _paid;
	private readonly IResponseCacheAppService _responses;

	public SendPaidReactionHandler(IPaidReactionsService paid, IResponseCacheAppService responses)
	{
		_paid = paid;
		_responses = responses;
	}

	protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Messages.RequestSendPaidReaction obj)
	{
		await _paid.ChargeAndRecordAsync(input.UserId, obj.Peer, obj.MsgId, obj.Count, obj.Private);
		return new MyTelegram.Schema.TUpdates { Updates = [], Users = [], Chats = [], Date = CurrentDate };
	}
}
