// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getFactCheck" />
///</summary>
internal sealed class GetFactCheckHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetFactCheck, TVector<MyTelegram.Schema.IFactCheck>>,
    Messages.IGetFactCheckHandler
{
    protected override Task<TVector<MyTelegram.Schema.IFactCheck>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetFactCheck obj)
    {
        throw new NotImplementedException();
    }
}
