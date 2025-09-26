namespace MyTelegram.ReadModel.Impl;

public class StoryReactionReadModel : IReadModel
{
    public string Id { get; set; } = null!; // {ownerPeerId}_{storyId}_{userId}
    public long OwnerPeerId { get; set; }
    public int StoryId { get; set; }
    public long UserId { get; set; }
    public IReaction Reaction { get; set; } = null!;
    public int Date { get; set; }
}

