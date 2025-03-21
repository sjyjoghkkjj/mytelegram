// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Get <a href="https://corefork.telegram.org/api/folders">folders</a>
/// See <a href="https://corefork.telegram.org/method/messages.getDialogFilters" />
///</summary>
internal sealed class GetDialogFiltersHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetDialogFilters, MyTelegram.Schema.Messages.IDialogFilters>,
    Messages.IGetDialogFiltersHandler
{
    private readonly IQueryProcessor _queryProcessor;
    private readonly IObjectMapper _objectMapper;
    public GetDialogFiltersHandler(IQueryProcessor queryProcessor,
        IObjectMapper objectMapper)
    {
        _queryProcessor = queryProcessor;
        _objectMapper = objectMapper;
    }

    protected override async Task<MyTelegram.Schema.Messages.IDialogFilters> HandleCoreAsync(IRequestInput input,
        RequestGetDialogFilters obj)
    {
        var filterReadModels = await _queryProcessor.ProcessAsync(new GetDialogFiltersQuery(input.UserId));

        var filters = new TVector<IDialogFilter>();
        filters.Add(new TDialogFilterDefault());

        foreach (var filterReadModel in filterReadModels)
        {
            var filter = _objectMapper.Map<DialogFilter, TDialogFilter>(filterReadModel.Filter);
            filters.Add(filter);
        }

        return new TDialogFilters
        {
            Filters = filters,
            TagsEnabled = true,
        };
    }
}
