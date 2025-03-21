namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Help;

///<summary>
/// Dismiss a <a href="https://corefork.telegram.org/api/config#suggestions">suggestion, see here for more info »</a>.
/// See <a href="https://corefork.telegram.org/method/help.dismissSuggestion" />
///</summary>
internal sealed class DismissSuggestionHandler : RpcResultObjectHandler<MyTelegram.Schema.Help.RequestDismissSuggestion, IBool>,
    Help.IDismissSuggestionHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Help.RequestDismissSuggestion obj)
    {
        return System.Threading.Tasks.Task.FromResult<IBool>(new TBoolTrue());
    }
}
