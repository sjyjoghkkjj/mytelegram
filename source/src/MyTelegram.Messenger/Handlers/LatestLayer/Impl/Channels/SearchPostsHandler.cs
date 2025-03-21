// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
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
