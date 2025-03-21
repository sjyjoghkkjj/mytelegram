namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Get <a href="https://corefork.telegram.org/api/folders">folders</a>
/// See <a href="https://corefork.telegram.org/method/messages.getDialogFilters" />
///</summary>
internal sealed class GetDialogFiltersHandler(
    IQueryProcessor queryProcessor,
    ILayeredService<IDialogFilterConverter> dialogFilterLayeredService)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetDialogFilters,
            MyTelegram.Schema.Messages.IDialogFilters>,
        Messages.IGetDialogFiltersHandler
{
    protected override async Task<MyTelegram.Schema.Messages.IDialogFilters> HandleCoreAsync(IRequestInput input,
        RequestGetDialogFilters obj)
    {
        var filterReadModels = await queryProcessor.ProcessAsync(new GetDialogFiltersQuery(input.UserId), CancellationToken.None);

        var filters = new TVector<IDialogFilter>
        {
            new TDialogFilterDefault()
        };
        var converter = dialogFilterLayeredService.GetConverter(input.Layer);
        foreach (var filterReadModel in filterReadModels)
        {
            var filter = converter.ToDialogFilter(filterReadModel.Filter);
            filters.Add(filter);
        }

        return new TDialogFilters
        {
            Filters = filters,
            TagsEnabled = true,
        };
    }
}
