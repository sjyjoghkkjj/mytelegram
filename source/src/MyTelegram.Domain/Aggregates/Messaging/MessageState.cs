using System.Collections.Concurrent;
using ReplyChannelMessageCompletedEvent = MyTelegram.Domain.Events.Messaging.ReplyChannelMessageCompletedEvent;

namespace MyTelegram.Domain.Aggregates.Messaging;

public class MessageState : AggregateState<MessageAggregate, MessageId, MessageState>,
    IApply<OutboxMessageCreatedEvent>,
    IApply<InboxMessageCreatedEvent>,
    IApply<InboxMessageIdAddedToOutboxMessageEvent>,
    IApply<MessageDeletedEvent>,
    IApply<OutboxMessageEditedEvent>,
    IApply<InboxMessageEditedEvent>,
    IApply<MessageForwardedEvent>,
    IApply<InboxMessageHasReadEvent>,
    IApply<ReplyToMessageEvent>,
    IApply<MessageViewsIncrementedEvent>,
    IApply<UpdatePinnedMessageStartedEvent>,
    IApply<InboxMessagePinnedUpdatedEvent>,
    IApply<OutboxMessagePinnedUpdatedEvent>,
    IApply<OtherPartyMessageDeletedEvent>,
    IApply<SelfMessageDeletedEvent>,
    IApply<OutboxMessageDeletedEvent>,
    IApply<InboxMessageDeletedEvent>,
    IApply<InboxItemsAddedToOutboxMessageEvent>,
    IApply<MessageDeleted4Event>,
    IApply<ReplyChannelMessageCompletedEvent>,
    IApply<ChannelMessagePinnedEvent>,
    IApply<ChannelMessageDeletedEvent>,
    IApply<MessageReplyUpdatedEvent>,
    IApply<MessageUnpinnedEvent>,
    IApply<MessagePinnedUpdatedEvent>,
    IApply<OutboxMessageEditedEventV2>,
    IApply<InboxMessageEditedEventV2>,
    IApply<MessageReactionAddedEvent>,
    IApply<MessageReactionRemovedEvent>,
    IApply<ScheduledMessageCreatedEvent>,
    IApply<ScheduledMessageSentEvent>,
    IApply<ScheduledMessageCancelledEvent>
{
    public int EditDate { get; private set; }
    //public bool EditHide { get; private set; }
    public bool Edited { get; private set; }

    public List<InboxItem> InboxItems { get; private set; } = new();
    public MessageItem MessageItem { get; private set; } = null!;
    public bool Pinned { get; private set; }
    public bool PmOneSide { get; private set; }
    public int Pts { get; private set; }
    public ConcurrentDictionary<long, ReactionCount> ReactionCounts { get; private set; } = new();
    public List<Reaction> RecentReactions { get; private set; } = new();

    public int SenderMessageId { get; private set; }
    public ConcurrentDictionary<long, List<Reaction>> UserReactions { get; } = new();

    public bool IsDeleted { get; private set; }
    public bool IsScheduled { get; private set; }
    public int? ScheduledDate { get; private set; }
    public void Apply(ChannelMessageDeletedEvent aggregateEvent)
    {
    }

    public void Apply(ChannelMessagePinnedEvent aggregateEvent)
    {
        Pinned = true;
    }

    public void Apply(InboxItemsAddedToOutboxMessageEvent aggregateEvent)
    {
        InboxItems = aggregateEvent.InboxItems;
        MessageItem = MessageItem with
        {
            InboxItems = InboxItems
        };
    }

    public void Apply(InboxMessageCreatedEvent aggregateEvent)
    {
        MessageItem = aggregateEvent.InboxMessageItem;
        SenderMessageId = aggregateEvent.SenderMessageId;
    }

    public void Apply(InboxMessageDeletedEvent aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(InboxMessageEditedEvent aggregateEvent)
    {
        EditDate = aggregateEvent.EditDate;
        Edited = true;
    }

    public void Apply(InboxMessageHasReadEvent aggregateEvent)
    {
    }

    public void Apply(InboxMessageIdAddedToOutboxMessageEvent aggregateEvent)
    {
        InboxItems.Add(aggregateEvent.InboxItem);
    }

    public void Apply(InboxMessagePinnedUpdatedEvent aggregateEvent)
    {
        Pinned = aggregateEvent.Pinned;
    }

    public void Apply(MessageDeleted4Event aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(MessageDeletedEvent aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(MessageForwardedEvent aggregateEvent)
    {
    }

    public void Apply(MessagePinnedUpdatedEvent aggregateEvent)
    {
        Pinned = aggregateEvent.Pinned;
    }

    public void Apply(MessageReplyUpdatedEvent aggregateEvent)
    {
        if (MessageItem.Reply != null)
        {
            MessageItem.Reply.ChannelId = aggregateEvent.ChannelId;
        }
    }

    public void Apply(MessageUnpinnedEvent aggregateEvent)
    {
        Pinned = false;
    }

    public void Apply(MessageViewsIncrementedEvent aggregateEvent)
    {
        MessageItem = MessageItem with { Views = MessageItem.Views + 1 };
    }

    public void Apply(OtherPartyMessageDeletedEvent aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(OutboxMessageCreatedEvent aggregateEvent)
    {
        MessageItem = aggregateEvent.OutboxMessageItem;
        SenderMessageId = aggregateEvent.OutboxMessageItem.MessageId;
    }

    public void Apply(OutboxMessageDeletedEvent aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(OutboxMessageEditedEvent aggregateEvent)
    {
        EditDate = aggregateEvent.EditDate;
        Edited = true;
    }

    public void Apply(OutboxMessagePinnedUpdatedEvent aggregateEvent)
    {
        Pinned = aggregateEvent.Pinned;
    }

    public void Apply(ReplyChannelMessageCompletedEvent aggregateEvent)
    {
        MessageItem = MessageItem with { Reply = aggregateEvent.Reply };
    }

    public void Apply(ReplyToMessageEvent aggregateEvent)
    {
    }

    public void Apply(SelfMessageDeletedEvent aggregateEvent)
    {
        IsDeleted = true;
    }

    public void Apply(UpdatePinnedMessageStartedEvent aggregateEvent)
    {
        Pinned = aggregateEvent.Pinned;
        PmOneSide = aggregateEvent.PmOneSide;
    }

    public void LoadSnapshot(MessageSnapshot snapshot)
    {
        MessageItem = snapshot.MessageItem;
        InboxItems = snapshot.InboxItems;
        SenderMessageId = snapshot.SenderMessageId;
        Pinned = snapshot.Pinned;
        EditDate = snapshot.EditDate;
        Edited = snapshot.Edited;
        Pts = snapshot.Pts;
        IsDeleted = snapshot.IsDeleted;
    }

    public void Apply(OutboxMessageEditedEventV2 aggregateEvent)
    {
        EditDate = aggregateEvent.NewMessageItem.EditDate ?? 0;
        Edited = true;

        MessageItem = aggregateEvent.NewMessageItem;
    }

    public void Apply(InboxMessageEditedEventV2 aggregateEvent)
    {
        EditDate = aggregateEvent.NewMessageItem.EditDate ?? 0;
        Edited = true;

        MessageItem = aggregateEvent.NewMessageItem;
    }

    public void Apply(MessageReactionAddedEvent aggregateEvent)
    {
        var reaction = new Reaction(aggregateEvent.Reaction);
        var reactionId = reaction.GetReactionId();
        
        // Add to user reactions
        if (!UserReactions.ContainsKey(aggregateEvent.UserId))
        {
            UserReactions[aggregateEvent.UserId] = new List<Reaction>();
        }
        
        // Remove existing reaction from this user if any
        UserReactions[aggregateEvent.UserId].RemoveAll(r => r.GetReactionId() == reactionId);
        UserReactions[aggregateEvent.UserId].Add(reaction);
        
        // Update reaction counts
        if (ReactionCounts.ContainsKey(reactionId))
        {
            ReactionCounts[reactionId].Count++;
        }
        else
        {
            ReactionCounts[reactionId] = new ReactionCount(reaction, 1, false);
        }
        
        // Add to recent reactions if requested
        if (aggregateEvent.AddToRecent)
        {
            RecentReactions.RemoveAll(r => r.GetReactionId() == reactionId);
            RecentReactions.Add(reaction);
        }
    }

    public void Apply(MessageReactionRemovedEvent aggregateEvent)
    {
        var reaction = new Reaction(aggregateEvent.Reaction);
        var reactionId = reaction.GetReactionId();
        
        // Remove from user reactions
        if (UserReactions.ContainsKey(aggregateEvent.UserId))
        {
            UserReactions[aggregateEvent.UserId].RemoveAll(r => r.GetReactionId() == reactionId);
        }
        
        // Update reaction counts
        if (ReactionCounts.ContainsKey(reactionId))
        {
            ReactionCounts[reactionId].Count--;
            if (ReactionCounts[reactionId].Count <= 0)
            {
                ReactionCounts.TryRemove(reactionId, out _);
            }
        }
    }

    public void Apply(ScheduledMessageCreatedEvent aggregateEvent)
    {
        IsScheduled = true;
        ScheduledDate = aggregateEvent.ScheduleDate;
        MessageItem = aggregateEvent.MessageItem;
    }

    public void Apply(ScheduledMessageSentEvent aggregateEvent)
    {
        IsScheduled = false;
        ScheduledDate = null;
        // Message is now sent, so it becomes a regular message
    }

    public void Apply(ScheduledMessageCancelledEvent aggregateEvent)
    {
        IsScheduled = false;
        ScheduledDate = null;
        IsDeleted = true;
    }
}