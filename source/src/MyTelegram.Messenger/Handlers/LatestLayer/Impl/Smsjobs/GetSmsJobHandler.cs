// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Smsjobs;

///<summary>
/// See <a href="https://corefork.telegram.org/method/smsjobs.getSmsJob" />
///</summary>
internal sealed class GetSmsJobHandler : RpcResultObjectHandler<MyTelegram.Schema.Smsjobs.RequestGetSmsJob, MyTelegram.Schema.ISmsJob>,
    Smsjobs.IGetSmsJobHandler
{
    protected override Task<MyTelegram.Schema.ISmsJob> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Smsjobs.RequestGetSmsJob obj)
    {
        throw new NotImplementedException();
    }
}
