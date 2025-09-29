namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Obtain available <a href="https://corefork.telegram.org/api/reactions">message reactions »</a>
/// See <a href="https://corefork.telegram.org/method/messages.getAvailableReactions" />
///</summary>
internal sealed class GetAvailableReactionsHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetAvailableReactions, MyTelegram.Schema.Messages.IAvailableReactions>
{
    protected override Task<MyTelegram.Schema.Messages.IAvailableReactions> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetAvailableReactions obj)
    {
        var thumbsUp = new TAvailableReaction
        {
            Reaction = "👍",
            Title = "Thumbs Up",
            StaticIcon = new TDocumentEmpty { Id = 0 },
            AppearAnimation = new TDocumentEmpty { Id = 0 },
            SelectAnimation = new TDocumentEmpty { Id = 0 },
            ActivateAnimation = new TDocumentEmpty { Id = 0 },
            EffectAnimation = new TDocumentEmpty { Id = 0 },
        };
        var thumbsDown = new TAvailableReaction
        {
            Reaction = "👎",
            Title = "Thumbs Down",
            StaticIcon = new TDocumentEmpty { Id = 0 },
            AppearAnimation = new TDocumentEmpty { Id = 0 },
            SelectAnimation = new TDocumentEmpty { Id = 0 },
            ActivateAnimation = new TDocumentEmpty { Id = 0 },
            EffectAnimation = new TDocumentEmpty { Id = 0 },
        };

        var r = new TAvailableReactions
        {
            Reactions = new TVector<IAvailableReaction>(thumbsUp, thumbsDown)
        };

        return Task.FromResult<IAvailableReactions>(r);
    }
}
