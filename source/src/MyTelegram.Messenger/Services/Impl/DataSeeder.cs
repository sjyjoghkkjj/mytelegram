namespace MyTelegram.Messenger.Services.Impl;

public class DataSeeder(
    ICommandBus commandBus,
    IRandomHelper randomHelper,
    IEventStore eventStore,
    ISnapshotStore snapshotStore)
    : IDataSeeder, ITransientDependency
{
    public async Task SeedAsync()
    {
        await CreateOfficialUserAsync();
        await CreateDefaultSupportUserAsync();
        await CreateGroupAnonymousBotUserAsync();
        var initUserId = MyTelegramServerDomainConsts.UserIdInitId;
        var testUserCount = 30;
        for (var i = 1; i < testUserCount; i++)
        {
            await CreateUserIfNeedAsync(initUserId + i,
                $"1{i}",
                $"{i}",
                $"{i}",
                $"user{i}",
                false);
        }
    }

    private async Task CreateDefaultSupportUserAsync()
    {
        var userId = MyTelegramServerDomainConsts.DefaultSupportUserId;
        var created = await CreateUserIfNeedAsync(userId,
            MyTelegramServerDomainConsts.DefaultSupportUserId.ToString(),
            "MyTelegram Support",
            null,
            null,
            false);

        if (created)
        {
            var command = new SetSupportCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(command, CancellationToken.None);

            var setVerifiedCommand = new SetVerifiedCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(setVerifiedCommand, CancellationToken.None);
        }
    }

    private async Task CreateGroupAnonymousBotUserAsync()
    {
        var userId = MyTelegramServerDomainConsts.GroupAnonymousBotUserId;
        var userName = "GroupAnonymousBot";
        await CreateUserIfNeedAsync(userId, "", "Group", null, userName, true);
    }

    private async Task CreateOfficialUserAsync()
    {
        var userId = MyTelegramServerDomainConsts.OfficialUserId;
        var created = await CreateUserIfNeedAsync(userId,
            "42777",
            "MyTelegram",
            null,
            null,
            false);

        if (created)
        {
            var command = new SetSupportCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(command, CancellationToken.None);

            var setVerifiedCommand = new SetVerifiedCommand(UserId.Create(userId), true);
            await commandBus.PublishAsync(setVerifiedCommand, CancellationToken.None);
        }
    }
    private async Task<bool> CreateUserIfNeedAsync(long userId,
        string phoneNumber,
        string firstName,
        string? lastName,
        string? userName,
        bool bot)
    {
        var aggregateId = UserId.Create(userId);
        var u = new UserAggregate(aggregateId);
        await u.LoadAsync(eventStore, snapshotStore, CancellationToken.None);
        if (u.IsNew)
        {
            var accessHash = randomHelper.NextInt64();
            var createUserCommand =
                new CreateUserCommand(aggregateId,
                    new RequestInfo(0, 0, 0, 0, Guid.Empty, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                    userId,
                    accessHash,
                    phoneNumber,
                    firstName,
                    lastName,
                    userName,
                    bot
                );
            await commandBus.PublishAsync(createUserCommand, CancellationToken.None);

            return true;
        }

        return false;
    }
}