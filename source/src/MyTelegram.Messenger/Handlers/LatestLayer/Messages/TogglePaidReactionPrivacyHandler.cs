using MyTelegram.Messenger.Services;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Changes paid reaction privacy for a specific message
/// See <a href="https://corefork.telegram.org/method/messages.togglePaidReactionPrivacy" />
/// </summary>
internal sealed class TogglePaidReactionPrivacyHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestTogglePaidReactionPrivacy, IBool>
{
	private readonly IPaidReactionsService _paid;

	public TogglePaidReactionPrivacyHandler(IPaidReactionsService paid)
	{
		_paid = paid;
	}

	protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
		MyTelegram.Schema.Messages.RequestTogglePaidReactionPrivacy obj)
	{
		await _paid.SetPrivacyAsync(input.UserId, obj.Peer, obj.MsgId, obj.Private);
		return new TBoolTrue();
	}
}
