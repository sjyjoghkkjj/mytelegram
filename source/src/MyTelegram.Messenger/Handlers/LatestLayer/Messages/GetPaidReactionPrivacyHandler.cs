using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Fetch default paid reaction privacy
/// See <a href="https://corefork.telegram.org/method/messages.getPaidReactionPrivacy" />
/// </summary>
internal sealed class GetPaidReactionPrivacyHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetPaidReactionPrivacy, MyTelegram.Schema.IUpdates>
{
	private readonly IPaidReactionsService _paid;

	public GetPaidReactionPrivacyHandler(IPaidReactionsService paid)
	{
		_paid = paid;
	}

	protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Messages.RequestGetPaidReactionPrivacy obj)
	{
		var privacy = await _paid.GetDefaultPrivacyAsync(input.UserId);
		var update = new TUpdatePaidReactionPrivacy { Private = privacy };
		return new TUpdates { Updates = [update], Chats = [], Date = CurrentDate, Users = [] };
	}
}
