using MyTelegram.Domain.Aggregates.Messaging;
using MyTelegram.Domain.Commands.Messaging;
using MyTelegram.Domain.Events.Messaging;
using MyTelegram.Domain.Tests;
using MyTelegram.Schema;

namespace MyTelegram.Domain.Tests;

public class ReactionTests : MyTelegramTestBase
{
    [Fact]
    public async Task SendReaction_ShouldEmitReactionAddedEvent()
    {
        // Arrange
        var messageId = MessageId.New;
        var aggregate = new MessageAggregate(messageId);
        var requestInfo = A<RequestInfo>();
        var userId = 123L;
        var reaction = new TReactionEmoji { Emoticon = "👍" };

        // Act
        aggregate.SendReaction(requestInfo, userId, reaction, true);

        // Assert
        var events = aggregate.GetUncommittedEvents();
        Assert.Single(events);
        var reactionEvent = events.First() as MessageReactionAddedEvent;
        Assert.NotNull(reactionEvent);
        Assert.Equal(userId, reactionEvent.UserId);
        Assert.Equal(reaction, reactionEvent.Reaction);
        Assert.True(reactionEvent.AddToRecent);
    }

    [Fact]
    public async Task RemoveReaction_ShouldEmitReactionRemovedEvent()
    {
        // Arrange
        var messageId = MessageId.New;
        var aggregate = new MessageAggregate(messageId);
        var requestInfo = A<RequestInfo>();
        var userId = 123L;
        var reaction = new TReactionEmoji { Emoticon = "👍" };

        // Act
        aggregate.RemoveReaction(requestInfo, userId, reaction);

        // Assert
        var events = aggregate.GetUncommittedEvents();
        Assert.Single(events);
        var reactionEvent = events.First() as MessageReactionRemovedEvent;
        Assert.NotNull(reactionEvent);
        Assert.Equal(userId, reactionEvent.UserId);
        Assert.Equal(reaction, reactionEvent.Reaction);
    }
}