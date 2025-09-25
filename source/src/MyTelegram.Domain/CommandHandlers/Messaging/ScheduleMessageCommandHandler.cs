namespace MyTelegram.Domain.CommandHandlers.Messaging;

public class ScheduleMessageCommandHandler : CommandHandler<MessageAggregate, MessageId, ScheduleMessageCommand>
{
    public override Task ExecuteAsync(MessageAggregate aggregate,
        ScheduleMessageCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.ScheduleMessage(command.RequestInfo,
            command.MessageItem,
            command.ScheduleDate,
            command.MentionedUserIds,
            command.ReplyToMsgItems,
            command.ClearDraft,
            command.GroupItemCount,
            command.LinkedChannelId,
            command.ChatMembers);
        return Task.CompletedTask;
    }
}