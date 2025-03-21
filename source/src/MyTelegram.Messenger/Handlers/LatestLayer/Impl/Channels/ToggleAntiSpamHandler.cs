// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Enable or disable the <a href="https://corefork.telegram.org/api/antispam">native antispam system</a>.
/// See <a href="https://corefork.telegram.org/method/channels.toggleAntiSpam" />
///</summary>
internal sealed class ToggleAntiSpamHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestToggleAntiSpam, MyTelegram.Schema.IUpdates>,
    Channels.IToggleAntiSpamHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestToggleAntiSpam obj)
    {
        RpcErrors.RpcErrors400.ChatNotModified.ThrowRpcError();
        return null!;
    }
}
