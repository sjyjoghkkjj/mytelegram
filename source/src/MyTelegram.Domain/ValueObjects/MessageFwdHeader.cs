namespace MyTelegram.Domain.ValueObjects;

public class MessageFwdHeader(
    bool imported,
    bool savedOut,
    Peer? fromId,
    string? fromName,
    int channelPost,
    string? postAuthor,
    int date,
    Peer? savedFromPeer,
    int? savedFromMsgId,
    Peer? savedFromId,
    string? savedFromName,
    int? savedDate,
    string? psaType,
    bool forwardFromLinkedChannel)
    : ValueObject
{
    /// <summary>
    ///     ID of the channel message that was forwarded
    /// </summary>
    public int ChannelPost { get; init; } = channelPost;

    /// <summary>
    ///     When was the message originally sent
    /// </summary>
    public int Date { get; init; } = date;

    public bool Imported { get; init; } = imported;
    public bool SavedOut { get; init; } = savedOut;

    /// <summary>
    ///     The ID of the user that originally sent the message
    /// </summary>
    public Peer? FromId { get; init; } = fromId;

    /// <summary>
    ///     The name of the user that originally sent the message
    /// </summary>
    public string? FromName { get; init; } = fromName;

    /// <summary>
    ///     For channels and if signatures are enabled, author of the channel message
    /// </summary>
    public string? PostAuthor { get; init; } = postAuthor;

    /// <summary>
    ///     Only for messages forwarded to the current user (inputPeerSelf), ID of the message that was forwarded from the
    ///     original user/channel
    /// </summary>
    public int? SavedFromMsgId { get; init; } = savedFromMsgId;

    public Peer? SavedFromId { get; init; } = savedFromId;
    public string? SavedFromName { get; init; } = savedFromName;
    public int? SavedDate { get; init; } = savedDate;
    public string? PsaType { get; } = psaType;
    public bool ForwardFromLinkedChannel { get; } = forwardFromLinkedChannel;

    /// <summary>
    ///     Only for messages forwarded to the current user (inputPeerSelf), full info about the user/channel that originally
    ///     sent the message
    /// </summary>
    public Peer? SavedFromPeer { get; init; } = savedFromPeer;
}
