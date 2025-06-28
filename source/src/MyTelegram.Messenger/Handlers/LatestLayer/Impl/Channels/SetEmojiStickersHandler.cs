// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// See <a href="https://corefork.telegram.org/method/channels.setEmojiStickers" />
///</summary>
internal sealed class SetEmojiStickersHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestSetEmojiStickers, IBool>,
    Channels.ISetEmojiStickersHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestSetEmojiStickers obj)
    {
        return Task.FromResult<IBool>(new MyTelegram.Schema.TBoolTrue());
    }
}
