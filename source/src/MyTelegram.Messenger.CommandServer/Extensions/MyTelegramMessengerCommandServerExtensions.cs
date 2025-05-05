using EventFlow.Core.Caching;
using EventFlow.MongoDB.Extensions;
using MyTelegram.Caching.Redis;
using MyTelegram.EventFlow;
using MyTelegram.EventFlow.MongoDB.Extensions;
using MyTelegram.Messenger.CommandServer.BackgroundServices;
using MyTelegram.Messenger.CommandServer.EventHandlers;
using MyTelegram.Messenger.NativeAot;
using MyTelegram.Messenger.Services.Impl;
using MyTelegram.QueryHandlers.MongoDB;
using MyTelegram.ReadModel.MongoDB;
using MyTelegram.Services.Extensions;
using MyTelegram.Services.NativeAot;

namespace MyTelegram.Messenger.CommandServer.Extensions;
public static class MyTelegramMessengerCommandServerExtensions
{
    public static void ConfigureEventBus(this IEventBus eventBus)
    {
        eventBus.Subscribe<MessengerCommandDataReceivedEvent, MessengerEventHandler>();
        eventBus.Subscribe<NewDeviceCreatedEvent, MessengerEventHandler>();
        eventBus.Subscribe<BindUidToAuthKeyIntegrationEvent, MessengerEventHandler>();
        eventBus.Subscribe<AuthKeyUnRegisteredIntegrationEvent, MessengerEventHandler>();

        eventBus.Subscribe<NewPtsMessageHasSentEvent, PtsEventHandler>();
        eventBus.Subscribe<RpcMessageHasSentEvent, PtsEventHandler>();
        eventBus.Subscribe<AcksDataReceivedEvent, PtsEventHandler>();

    }

    public static void AddMyTelegramMessengerCommandServer(this IServiceCollection services,
        Action<IEventFlowOptions>? configure = null)
    {
        services.RegisterServices();
        services.AddMyTelegramMessengerServices();

        services.AddEventFlow(options =>
        {
            options.AddDefaults(typeof(MyTelegramMessengerServerExtensions).Assembly);
            options.AddDefaults(typeof(MyTelegram.Domain.Specs).Assembly);
            options.Configure(c => { c.IsAsynchronousSubscribersEnabled = true; });

            options.UseMongoDbEventStore();
            options.UseMongoDbSnapshotStore();

            options.AddMyTelegramMongoDbReadModel();
            options.AddMongoDbQueryHandlers();

            options.AddSystemTextJson(jsonSerializerOptions =>
            {
                jsonSerializerOptions.AddSingleValueObjects(
                    new EventFlow.SystemTextJsonSingleValueObjectConverter<CacheKey>());
                jsonSerializerOptions.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
            });
            configure?.Invoke(options);
        });
		services.AddEventStoreMongoDbContext();
        services.AddReadModelMongoDbContext();
    }
}
