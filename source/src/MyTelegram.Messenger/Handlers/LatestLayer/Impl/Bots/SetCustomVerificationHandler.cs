namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Bots;

///<summary>
/// See <a href="https://corefork.telegram.org/method/bots.setCustomVerification" />
///</summary>
internal sealed class SetCustomVerificationHandler : RpcResultObjectHandler<MyTelegram.Schema.Bots.RequestSetCustomVerification, IBool>,
    Bots.ISetCustomVerificationHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Bots.RequestSetCustomVerification obj)
    {
        throw new NotImplementedException();
    }
}
