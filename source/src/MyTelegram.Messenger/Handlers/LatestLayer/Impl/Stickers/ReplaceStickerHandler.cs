// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Stickers;

///<summary>
/// See <a href="https://corefork.telegram.org/method/stickers.replaceSticker" />
///</summary>
internal sealed class ReplaceStickerHandler : RpcResultObjectHandler<MyTelegram.Schema.Stickers.RequestReplaceSticker, MyTelegram.Schema.Messages.IStickerSet>,
    Stickers.IReplaceStickerHandler
{
    protected override Task<MyTelegram.Schema.Messages.IStickerSet> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Stickers.RequestReplaceSticker obj)
    {
        throw new NotImplementedException();
    }
}
