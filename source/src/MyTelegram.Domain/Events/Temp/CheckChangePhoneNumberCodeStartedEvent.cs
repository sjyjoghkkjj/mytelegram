namespace MyTelegram.Domain.Events.Temp;

public class CheckChangePhoneNumberCodeStartedEvent(RequestInfo requestInfo, string phoneNumber, string phoneCodeHash, string code) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public string PhoneNumber { get; } = phoneNumber;
    public string PhoneCodeHash { get; } = phoneCodeHash;
    public string Code { get; } = code;
}

public class SendMessageStartedEvent(RequestInfo requestInfo, List<SendMessageItem> sendMessageItems, bool isSendQuickReplyMessages,
    bool isSendGroupedMessages,
    bool clearDraft) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public List<SendMessageItem> SendMessageItems { get; } = sendMessageItems;
    public bool IsSendQuickReplyMessages { get; } = isSendQuickReplyMessages;
    public bool IsSendGroupedMessages { get; } = isSendGroupedMessages;
    public bool ClearDraft { get; } = clearDraft;
}

public class SendScheduleMessageStartedEvent(
    RequestInfo requestInfo,
    List<SendMessageItem> sendMessageItems,
    bool isSendGroupedMessages) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public List<SendMessageItem> SendMessageItems { get; } = sendMessageItems;
    public bool IsSendGroupedMessages { get; } = isSendGroupedMessages;
}

public class DeleteScheduleMessagesStartedEvent(RequestInfo requestInfo, long ownerPeerId, Peer toPeer, List<int> messageIds) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public long OwnerPeerId { get; } = ownerPeerId;
    public Peer ToPeer { get; } = toPeer;
    public List<int> MessageIds { get; } = messageIds;
}

public class ToggleStoryPinnedStartedEvent(RequestInfo requestInfo, Peer peer, List<int> storyIds, bool pinned) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer Peer { get; } = peer;
    public List<int> StoryIds { get; } = storyIds;
    public bool Pinned { get; } = pinned;
}

public class TogglePinnedToTopStartedEvent(RequestInfo requestInfo, Peer peer, List<int> storyIds) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer Peer { get; } = peer;
    public List<int> StoryIds { get; } = storyIds;
}

public class DeleteStoriesStartedEvent(RequestInfo requestInfo, Peer peer, List<int> storyIds) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer Peer { get; } = peer;
    public List<int> StoryIds { get; } = storyIds;
}

public class InviteToChannelStartedEvent(RequestInfo requestInfo,
    long channelId,
    bool isBroadcast,
    bool hasLink,
    long inviterId,
    int channelHistoryMinId,
    int maxMessageId,
    List<long> memberUserIds,
    List<long> botUserIds,
    ChatJoinType chatJoinType) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public long ChannelId { get; } = channelId;
    public bool IsBroadcast { get; } = isBroadcast;
    public bool HasLink { get; } = hasLink;
    public long InviterId { get; } = inviterId;
    public int ChannelHistoryMinId { get; } = channelHistoryMinId;
    public int MaxMessageId { get; } = maxMessageId;
    public List<long> MemberUserIds { get; } = memberUserIds;
    public List<long> BotUserIds { get; } = botUserIds;
    public ChatJoinType ChatJoinType { get; } = chatJoinType;
}

public class ReadStoriesStartedEvent(RequestInfo requestInfo, Peer peer, List<int> storyIds) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer Peer { get; } = peer;
    public List<int> StoryIds { get; } = storyIds;
}

public class IncrementStoryViewStartedEvent(RequestInfo requestInfo, Peer peer, List<int> storyIds) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer Peer { get; } = peer;
    public List<int> StoryIds { get; } = storyIds;
}

public class SendStoryReactionStartedEvent(RequestInfo requestInfo, Peer ownerPeer, int storyId, IReaction reaction) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public Peer OwnerPeer { get; } = ownerPeer;
    public int StoryId { get; } = storyId;
    public IReaction Reaction { get; } = reaction;
}