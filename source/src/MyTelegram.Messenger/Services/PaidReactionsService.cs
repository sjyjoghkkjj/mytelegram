using MyTelegram.Schema;

namespace MyTelegram.Messenger.Services;

public interface IPaidReactionsService : ITransientDependency
{
    Task<bool> ChargeAndRecordAsync(long userId, IInputPeer peer, int msgId, int count, IPaidReactionPrivacy? privacy);
    Task<IPaidReactionPrivacy> GetDefaultPrivacyAsync(long userId);
    Task SetPrivacyAsync(long userId, IInputPeer peer, int msgId, IPaidReactionPrivacy privacy);
    IReadOnlyCollection<IMessageReactor> GetTopReactors(long chatId, int msgId, int limit = 10);
}

public class PaidReactionsService(ILogger<PaidReactionsService> logger, ICommandBus commandBus) : IPaidReactionsService
{
    private readonly ConcurrentDictionary<(long UserId, long ChatId, int MsgId), IPaidReactionPrivacy> _privacy = new();
    private readonly ConcurrentDictionary<(long ChatId, int MsgId), ConcurrentDictionary<long, int>> _counters = new();

    public async Task<bool> ChargeAndRecordAsync(long userId, IInputPeer peer, int msgId, int count, IPaidReactionPrivacy? privacy)
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
        // списание звёзд
        var now = DateTime.UtcNow.ToTimestamp();
        var txId = Guid.NewGuid().ToString("N");
        await commandBus.PublishAsync(new DebitStarsCommand(StarsAccountId.Create(userId), RequestInfo.Empty with { UserId = userId }, userId, count, "STAR", "paid_reaction", txId, now));

        // учёт лидерборда
        var map = _counters.GetOrAdd((chatId, msgId), _ => new ConcurrentDictionary<long, int>());
        map.AddOrUpdate(userId, count, (_, old) => old + count);

        logger.LogInformation("Paid reaction: user={UserId} chat={ChatId} msg={MsgId} count={Count}", userId, chatId, msgId, count);
        return true;
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

    public IReadOnlyCollection<IMessageReactor> GetTopReactors(long chatId, int msgId, int limit = 10)
    {
        if (_counters.TryGetValue((chatId, msgId), out var map))
        {
            return map
                .OrderByDescending(kv => kv.Value)
                .Take(limit)
                .Select(kv => (IMessageReactor)new TMessageReactor
                {
                    Top = false,
                    My = false,
                    Anonymous = false,
                    PeerId = new TPeerUser { UserId = kv.Key },
                    Count = kv.Value
                }).ToList();
        }
        return Array.Empty<IMessageReactor>();
    }
}

