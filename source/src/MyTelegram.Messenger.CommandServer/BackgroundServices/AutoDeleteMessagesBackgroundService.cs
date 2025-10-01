using Microsoft.Extensions.Hosting;

namespace MyTelegram.Messenger.CommandServer.BackgroundServices;

public class AutoDeleteMessagesBackgroundService(
    ILogger<AutoDeleteMessagesBackgroundService> logger,
    IQueryProcessor queryProcessor,
    ICommandBus commandBus)
    : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AutoDeleteMessages service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var expired = await queryProcessor.ProcessAsync(new GetAutoDeleteMessagesQuery((int)now, 0, BatchSize), stoppingToken);
                if (expired.Count > 0)
                {
                    foreach (var item in expired)
                    {
                        try
                        {
                            var aggregateId = MessageId.Create(item.OwnerPeerId, item.MessageId);
                            await commandBus.PublishAsync(new DeleteMessageCommand(aggregateId, RequestInfo.Empty), stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to delete expired message {OwnerPeerId}/{MessageId}", item.OwnerPeerId, item.MessageId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AutoDeleteMessages iteration failed");
            }

            try
            {
                await Task.Delay(Delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        logger.LogInformation("AutoDeleteMessages service stopped");
    }
}

