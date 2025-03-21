// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getEmojiStickerGroups" />
///</summary>
internal sealed class GetEmojiStickerGroupsHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetEmojiStickerGroups, MyTelegram.Schema.Messages.IEmojiGroups>,
    Messages.IGetEmojiStickerGroupsHandler
{
    protected override Task<MyTelegram.Schema.Messages.IEmojiGroups> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetEmojiStickerGroups obj)
    {
        return Task.FromResult<MyTelegram.Schema.Messages.IEmojiGroups>(new TEmojiGroups
        {
            Groups = []
        });
    }
}
