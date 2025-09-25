using MyTelegram.Domain.Aggregates.Messaging;
using MyTelegram.Domain.Commands.Messaging;
using MyTelegram.Domain.Events.Messaging;
using MyTelegram.Domain.Tests;
using MyTelegram.Domain.Shared;

namespace MyTelegram.Domain.Tests;

public class ScheduledMessageTests : MyTelegramTestBase
{
    [Fact]
    public async Task ScheduleMessage_ShouldEmitScheduledMessageCreatedEvent()
    {
        // Arrange
        var messageId = MessageId.New;
        var aggregate = new MessageAggregate(messageId);
        var requestInfo = A<RequestInfo>();
        var messageItem = A<MessageItem>();
        var scheduleDate = (int)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        // Act
        aggregate.ScheduleMessage(requestInfo, messageItem, scheduleDate);

        // Assert
        var events = aggregate.GetUncommittedEvents();
        Assert.Single(events);
        var scheduledEvent = events.First() as ScheduledMessageCreatedEvent;
        Assert.NotNull(scheduledEvent);
        Assert.Equal(scheduleDate, scheduledEvent.ScheduleDate);
        Assert.Equal(messageItem, scheduledEvent.MessageItem);
    }

    [Fact]
    public async Task CancelScheduledMessage_ShouldEmitScheduledMessageCancelledEvent()
    {
        // Arrange
        var messageId = MessageId.New;
        var aggregate = new MessageAggregate(messageId);
        var requestInfo = A<RequestInfo>();
        var scheduleDate = (int)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        // Act
        aggregate.CancelScheduledMessage(requestInfo, scheduleDate);

        // Assert
        var events = aggregate.GetUncommittedEvents();
        Assert.Single(events);
        var cancelledEvent = events.First() as ScheduledMessageCancelledEvent;
        Assert.NotNull(cancelledEvent);
        Assert.Equal(scheduleDate, cancelledEvent.ScheduleDate);
    }

    [Fact]
    public async Task SendScheduledMessage_ShouldEmitScheduledMessageSentEvent()
    {
        // Arrange
        var messageId = MessageId.New;
        var aggregate = new MessageAggregate(messageId);
        var requestInfo = A<RequestInfo>();
        var messageItem = A<MessageItem>();
        var scheduleDate = (int)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        // Act
        aggregate.SendScheduledMessage(requestInfo, messageItem, scheduleDate);

        // Assert
        var events = aggregate.GetUncommittedEvents();
        Assert.Single(events);
        var sentEvent = events.First() as ScheduledMessageSentEvent;
        Assert.NotNull(sentEvent);
        Assert.Equal(scheduleDate, sentEvent.ScheduleDate);
        Assert.Equal(messageItem, sentEvent.MessageItem);
    }
}