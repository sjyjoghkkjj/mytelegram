namespace MyTelegram.ReadModel.Impl;

public class ForumTopicReadModel : IForumTopicReadModel,
    IAmReadModelFor<ChannelAggregate, ForumTopicId, ForumTopicCreatedEvent>,
    IAmReadModelFor<ChannelAggregate, ForumTopicId, ForumTopicEditedEvent>,
    IAmReadModelFor<ChannelAggregate, ForumTopicId, ForumTopicPinnedEvent>,
    IAmReadModelFor<ChannelAggregate, ForumTopicId, ForumTopicPinnedOrderChangedEvent>
{
    public string Id { get; private set; } = null!;
    public long ChannelId { get; private set; }
    public int TopicId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public long? IconEmojiId { get; private set; }
    public int? IconColor { get; private set; }
    public Peer? SendAs { get; private set; }
    public bool Pinned { get; private set; }
    public int Date { get; private set; }
    public int TopMessage { get; private set; }
    public bool My { get; private set; }
    public bool Closed { get; private set; }
    public int ReadInboxMaxId { get; private set; }
    public int ReadOutboxMaxId { get; private set; }
    public int UnreadCount { get; private set; }
    public int UnreadMentionsCount { get; private set; }
    public int UnreadReactionsCount { get; private set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ChannelAggregate, ForumTopicId, ForumTopicCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        ChannelId = domainEvent.AggregateEvent.ChannelId;
        TopicId = domainEvent.AggregateEvent.TopicId;
        Title = domainEvent.AggregateEvent.Title;
        IconEmojiId = domainEvent.AggregateEvent.IconEmojiId;
        IconColor = domainEvent.AggregateEvent.IconColor;
        SendAs = domainEvent.AggregateEvent.SendAs;
        Date = domainEvent.AggregateEvent.Date;
        TopMessage = domainEvent.AggregateEvent.TopMessageId;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ChannelAggregate, ForumTopicId, ForumTopicEditedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Title = domainEvent.AggregateEvent.Title ?? Title;
        IconEmojiId = domainEvent.AggregateEvent.IconEmojiId ?? IconEmojiId;
        IconColor = domainEvent.AggregateEvent.IconColor ?? IconColor;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ChannelAggregate, ForumTopicId, ForumTopicPinnedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Pinned = domainEvent.AggregateEvent.Pinned;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ChannelAggregate, ForumTopicId, ForumTopicPinnedOrderChangedEvent> domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

