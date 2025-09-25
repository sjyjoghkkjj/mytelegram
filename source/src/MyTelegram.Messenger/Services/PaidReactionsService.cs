using MyTelegram.Schema;

namespace MyTelegram.Messenger.Services;

public interface IPaidReactionsService : ITransientDependency
{
    Task<bool> ChargeAndRecordAsync(long userId, IInputPeer peer, int msgId, int count, IPaidReactionPrivacy? privacy);
    Task<IPaidReactionPrivacy> GetDefaultPrivacyAsync(long userId);
    Task SetPrivacyAsync(long userId, IInputPeer peer, int msgId, IPaidReactionPrivacy privacy);
}

public class PaidReactionsService(ILogger<PaidReactionsService> logger) : IPaidReactionsService
{
    private readonly ConcurrentDictionary<(long UserId, long ChatId, int MsgId), IPaidReactionPrivacy> _privacy = new();

    public Task<bool> ChargeAndRecordAsync(long userId, IInputPeer peer, int msgId, int count, IPaidReactionPrivacy? privacy)
    {
        // Списания звёзд интегрируются со Stars балансом; тут — заглушка на запись приватности
        var chatId = peer switch
        {
            TInputPeerChannel ch => ch.ChannelId,
            TInputPeerChat c => c.ChatId,
            TInputPeerUser u => u.UserId,
            _ => 0
        };
        if (privacy != null)
        {
            _privacy[(userId, chatId, msgId)] = privacy;
        }
        logger.LogInformation("Paid reaction: user={UserId} chat={ChatId} msg={MsgId} count={Count}", userId, chatId, msgId, count);
        return Task.FromResult(true);
    }

    public Task<IPaidReactionPrivacy> GetDefaultPrivacyAsync(long userId)
    {
        return Task.FromResult<IPaidReactionPrivacy>(new TPaidReactionPrivacyDefault());
    }

    public Task SetPrivacyAsync(long userId, IInputPeer peer, int msgId, IPaidReactionPrivacy privacy)
    {
        var chatId = peer switch
        {
            TInputPeerChannel ch => ch.ChannelId,
            TInputPeerChat c => c.ChatId,
            TInputPeerUser u => u.UserId,
            _ => 0
        };
        _privacy[(userId, chatId, msgId)] = privacy;
        return Task.CompletedTask;
    }
}

