using MyTelegram.Domain.Aggregates.Messaging;
using MyTelegram.Domain.Commands.Messaging;
using MyTelegram.Domain.Shared;
using MyTelegram.ReadModel.Interfaces;

namespace MyTelegram.Services.Services;

public class ScheduledMessageService(
    ICommandBus commandBus,
    IQueryProcessor queryProcessor,
    IScheduleAppService scheduleAppService,
    ILogger<ScheduledMessageService> logger) : IScheduledMessageService, ITransientDependency
{
    public async Task ScheduleMessageAsync(MessageId messageId, MessageItem messageItem, int scheduleDate)
    {
        var command = new ScheduleMessageCommand(messageId, RequestInfo.Empty, messageItem, scheduleDate);
        await commandBus.PublishAsync(command);
        
        // Schedule the message to be sent at the specified time
        var delay = TimeSpan.FromSeconds(scheduleDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (delay > TimeSpan.Zero)
        {
            scheduleAppService.Execute(() => SendScheduledMessageAsync(messageId, messageItem, scheduleDate), delay);
        }
    }

    public async Task CancelScheduledMessageAsync(MessageId messageId, int scheduleDate)
    {
        var command = new CancelScheduledMessageCommand(messageId, RequestInfo.Empty, scheduleDate);
        await commandBus.PublishAsync(command);
    }

    public async Task SendScheduledMessageAsync(MessageId messageId, MessageItem messageItem, int scheduleDate)
    {
        try
        {
            // Create a new message aggregate for the scheduled message
            var scheduledMessageId = MessageId.New;
            var scheduleCommand = new ScheduleMessageCommand(scheduledMessageId, RequestInfo.Empty, messageItem, scheduleDate);
            await commandBus.PublishAsync(scheduleCommand);
            
            logger.LogInformation("Scheduled message sent: {MessageId} at {ScheduleDate}", messageId, scheduleDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send scheduled message: {MessageId}", messageId);
        }
    }

    public async Task<List<ScheduledMessageInfo>> GetScheduledMessagesAsync(long userId, long peerId)
    {
        // This would typically query the read model for scheduled messages
        // For now, return empty list as we need to implement the read model
        return new List<ScheduledMessageInfo>();
    }
}