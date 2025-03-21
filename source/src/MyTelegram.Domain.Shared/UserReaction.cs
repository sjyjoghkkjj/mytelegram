namespace MyTelegram;

public record UserReaction(long UserId,
    List<long> ReactionIds);