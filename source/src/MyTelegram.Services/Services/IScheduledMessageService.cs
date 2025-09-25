namespace MyTelegram.Services.Services;

public interface IScheduledMessageService
{
    Task ScheduleMessageAsync(MessageId messageId, MessageItem messageItem, int scheduleDate);
    Task CancelScheduledMessageAsync(MessageId messageId, int scheduleDate);
    Task SendScheduledMessageAsync(MessageId messageId, MessageItem messageItem, int scheduleDate);
    Task<List<ScheduledMessageInfo>> GetScheduledMessagesAsync(long userId, long peerId);
}

public record ScheduledMessageInfo(
    MessageId MessageId,
    MessageItem MessageItem,
    int ScheduleDate,
    bool IsActive);