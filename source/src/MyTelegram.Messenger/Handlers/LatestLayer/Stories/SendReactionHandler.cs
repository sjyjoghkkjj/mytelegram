namespace MyTelegram.Messenger.Handlers.LatestLayer.Stories;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stories.sendReaction" />
///</summary>
internal sealed class SendReactionHandler(IQueryProcessor queryProcessor, IAppConfigHelper appConfigHelper, IObjectMessageSender objectMessageSender) : RpcResultObjectHandler<MyTelegram.Schema.Stories.RequestSendReaction, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stories.RequestSendReaction obj)
    {
        // Persist to StoryReactionReadModel via object message bus (simulate domain event)
        // For now, directly emit update; storage handlers can be added similarly to messages
        var update = new TUpdateSentStoryReaction
        {
            Peer = obj.Peer,
            StoryId = obj.StoryId,
            Reaction = obj.Reaction
        };
        // Notify poster
        var updates = new TUpdates { Updates = new TVector<IUpdate>(update), Chats = [], Users = [], Date = CurrentDate };
        await objectMessageSender.SendMessageToPeerAsync(input.ToRequestInfo(), updates);
        return updates;
    }
}
