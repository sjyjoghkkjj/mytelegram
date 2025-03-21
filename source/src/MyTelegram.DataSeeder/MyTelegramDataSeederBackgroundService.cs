using EventFlow.Queries;
using MyTelegram.Queries;

namespace MyTelegram.DataSeeder;

internal class MyTelegramDataSeederBackgroundService(
    IDataSeederService dataSeederService,
    IDataSeederHelper dataSeederHelper,
    IQueryProcessor queryProcessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var myTelegramUserReadModel =
            await queryProcessor.ProcessAsync(new GetUserByIdQuery(MyTelegramServerDomainConsts.OfficialUserId),
                stoppingToken);
        if (myTelegramUserReadModel == null)
        {
            await dataSeederHelper.ResetDataSeederConfigAsync();
        }
        //var aggregate = new UserAggregate(UserId.Create(MyTelegramServerDomainConsts.OfficialUserId));
        //await aggregate.LoadAsync(eventStore, snapshotStore, stoppingToken);
        //if (aggregate.IsNew)
        //{
        //    await dataSeederHelper.ResetDataSeederConfigAsync();
        //}

        await dataSeederService.SeedAllAsync();
    }
}