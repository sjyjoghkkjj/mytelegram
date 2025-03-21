// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Report a <a href="https://corefork.telegram.org/api/antispam">native antispam</a> false positive
/// See <a href="https://corefork.telegram.org/method/channels.reportAntiSpamFalsePositive" />
///</summary>
internal sealed class ReportAntiSpamFalsePositiveHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestReportAntiSpamFalsePositive, IBool>,
    Channels.IReportAntiSpamFalsePositiveHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestReportAntiSpamFalsePositive obj)
    {
        return Task.FromResult<IBool>(new TBoolTrue());
    }
}
