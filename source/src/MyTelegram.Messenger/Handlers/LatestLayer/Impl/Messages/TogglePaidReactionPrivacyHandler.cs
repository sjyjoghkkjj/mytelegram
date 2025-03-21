// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.togglePaidReactionPrivacy" />
///</summary>
internal sealed class TogglePaidReactionPrivacyHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestTogglePaidReactionPrivacy, IBool>,
    Messages.ITogglePaidReactionPrivacyHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestTogglePaidReactionPrivacy obj)
    {
        throw new NotImplementedException();
    }
}
