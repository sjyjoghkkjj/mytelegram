namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Globally search for posts from public <a href="https://corefork.telegram.org/api/channel">channels »</a> (<em>including</em> those we aren't a member of) containing a specific hashtag.
/// See <a href="https://corefork.telegram.org/method/channels.searchPosts" />
///</summary>
internal sealed class SearchPostsHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestSearchPosts, MyTelegram.Schema.Messages.IMessages>,
    Channels.ISearchPostsHandler
{
    protected override Task<MyTelegram.Schema.Messages.IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestSearchPosts obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.IMessages>(new TMessages
        {
            Chats = [],
            Messages = [],
            Users = []
        });
    }
}
