namespace MyTelegram.Messenger.Handlers.LatestLayer.Stories;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stories.sendReaction" />
///</summary>
internal sealed class SendReactionHandler(IQueryProcessor queryProcessor, IAppConfigHelper appConfigHelper) : RpcResultObjectHandler<MyTelegram.Schema.Stories.RequestSendReaction, MyTelegram.Schema.IUpdates>
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stories.RequestSendReaction obj)
    {
        var update = new TUpdateSentStoryReaction
        {
            Peer = obj.Peer,
            StoryId = obj.StoryId,
            Reaction = obj.Reaction
        };
        return Task.FromResult<IUpdates>(new TUpdates { Updates = new TVector<IUpdate>(update), Chats = [], Users = [], Date = CurrentDate });
    }
}
