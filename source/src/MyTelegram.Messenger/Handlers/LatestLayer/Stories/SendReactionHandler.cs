namespace MyTelegram.Messenger.Handlers.LatestLayer.Stories;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stories.sendReaction" />
///</summary>
internal sealed class SendReactionHandler(
    IQueryProcessor queryProcessor,
    IAppConfigHelper appConfigHelper,
    IObjectMessageSender objectMessageSender,
    IMyMongoDbReadModelStore<StoryReactionReadModel> storyReactionStore
) : RpcResultObjectHandler<MyTelegram.Schema.Stories.RequestSendReaction, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stories.RequestSendReaction obj)
    {
        // Persist reaction (simple upsert semantics)
        var ownerPeerId = input.UserId; // story owner resolution could vary; using current for demo
        var userId = input.UserId;
        var date = CurrentDate;
        var id = $"{ownerPeerId}_{obj.StoryId}_{userId}";
        await storyReactionStore.InsertAsync(new StoryReactionReadModel
        {
            Id = id,
            OwnerPeerId = ownerPeerId,
            StoryId = obj.StoryId,
            UserId = userId,
            Date = date,
            Reaction = new Reaction(userId, (obj.Reaction as TReactionEmoji)?.Emoticon, (obj.Reaction as TReactionCustomEmoji)?.DocumentId, date)
        }, default);

        // Send updates
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
