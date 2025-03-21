// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// See <a href="https://corefork.telegram.org/method/channels.getChannelRecommendations" />
///</summary>
internal sealed class GetChannelRecommendationsHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetChannelRecommendations, MyTelegram.Schema.Messages.IChats>,
    Channels.IGetChannelRecommendationsHandler
{
    protected override Task<MyTelegram.Schema.Messages.IChats> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetChannelRecommendations obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.IChats>(new TChats
        {
            Chats = new()
        });
    }
}

//internal sealed class GetChannelRecommendationsHandler2 : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetChannelRecommendations2, MyTelegram.Schema.Messages.IChats>,
//    Channels.IGetChannelRecommendationsHandler2
//{
//    protected override Task<MyTelegram.Schema.Messages.IChats> HandleCoreAsync(IRequestInput input,
//        MyTelegram.Schema.Channels.RequestGetChannelRecommendations2 obj)
//    {
//        return Task.FromResult<MyTelegram.Schema.Messages.IChats>(new TChats
//        {
//            Chats = new()
//        });
//    }
//}
