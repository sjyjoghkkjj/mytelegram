using EventFlow.MongoDB.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MyTelegram.Caching.Redis;
using MyTelegram.Domain.Aggregates.Updates;
using MyTelegram.Domain.CommandHandlers.Pts;
using MyTelegram.Domain.CommandHandlers.PushUpdates;
using MyTelegram.Domain.CommandHandlers.RpcResult;
using MyTelegram.Domain.CommandHandlers.Updates;
using MyTelegram.Domain.Commands.Pts;
using MyTelegram.Domain.Commands.PushUpdates;
using MyTelegram.Domain.Commands.RpcResult;
using MyTelegram.Domain.Commands.Updates;
using MyTelegram.Domain.Events.PushUpdates;
using MyTelegram.Domain.Events.RpcResult;
using MyTelegram.Domain.Events.Updates;
using MyTelegram.EventFlow.Extensions;
using MyTelegram.EventFlow.MongoDB.ReadStores;
using MyTelegram.EventFlow.ReadStores;
using MyTelegram.Messenger.Extensions;
using MyTelegram.Messenger.NativeAot;
using MyTelegram.Messenger.QueryServer.EventHandlers;
using MyTelegram.Messenger.QueryServer.Services;
using MyTelegram.Messenger.Services.Impl;
using MyTelegram.QueryHandlers.MongoDB;
using MyTelegram.ReadModel.MongoDB;
using MyTelegram.ReadModel.ReadModelLocators;
using MyTelegram.Services.NativeAot;

namespace MyTelegram.Messenger.QueryServer.Extensions;

public static class MyTelegramMessengerQueryServerExtensions
{
    public static void ConfigureEventBus(this IEventBus eventBus)
    {
        eventBus.Subscribe<MessengerQueryDataReceivedEvent, MessengerEventHandler>();
        eventBus.Subscribe<StickerDataReceivedEvent, MessengerEventHandler>();

        eventBus.Subscribe<UserIsOnlineEvent, UserIsOnlineEventHandler>();

        eventBus.Subscribe<DomainEventMessage, DistributedDomainEventHandler>();
        eventBus.Subscribe<DuplicateCommandEvent, DuplicateOperationExceptionHandler>();
    }

    public static void AddMyTelegramMessengerQueryServer(this IServiceCollection services, Action<IEventFlowOptions>? configure = null)
    {
        services.AddTransient<IDataProcessor<IDomainEvent>, DomainEventDataProcessor>();
        services.AddTransient<IChatInviteLinkHelper, ChatInviteLinkHelper>();

        services.RegisterServices();
        services.AddTransient<IPtsForAuthKeyIdReadModelLocator, PtsForAuthKeyIdReadModelLocator>();

        services.AddEventFlow(options =>
        {
            options.AddMessengerMongoDbReadModel();
            options.AddQueryHandlers();
            options.AddEvents(typeof(MyTelegram.Domain.Aggregates.AppCode.AppCodeAggregate).Assembly);
            options.AddEventUpgraders();
            options.AddMongoDbQueryHandlers();
            options.AddSubscribers(Assembly.GetEntryAssembly());

            options.AddCommands(
                typeof(CreateRpcResultCommand),
                typeof(CreatePushUpdatesCommand),
                typeof(CreateUpdatesCommand),
                typeof(CreateEncryptedPushUpdatesCommand),
                typeof(UpdatePtsCommand),
                typeof(PtsAckedCommand),
                typeof(QtsAckedCommand),
                typeof(IncrementTempPtsCommand),
                typeof(UpdateQtsCommand),
                typeof(UpdatePtsForAuthKeyIdCommand),
                typeof(UpdateGlobalSeqNoCommand)
                //typeof(CreatePtsCommand)
                );
            options.AddCommandHandlers(
                typeof(CreateRpcResultCommandHandler),
                typeof(CreateUpdatesCommandHandler),
                typeof(CreateEncryptedPushUpdatesCommandHandler),
                typeof(UpdatePtsCommandHandler),
                typeof(PtsAckedCommandHandler),
                typeof(QtsAckedCommandHandler),
                typeof(IncrementTempPtsCommandHandler),
                typeof(UpdateQtsCommandHandler),
                typeof(UpdatePtsForAuthKeyIdCommandHandler),
                typeof(UpdateGlobalSeqNoCommandHandler)
            );
            options.AddEvents(
                typeof(EncryptedPushUpdatesCreatedEvent),
                typeof(PushUpdatesCreatedEvent),
                typeof(RpcResultCreatedEvent),
                typeof(UpdatesCreatedEvent),
                typeof(PtsUpdatedEvent),
                typeof(PtsAckedEvent),
                typeof(TempPtsIncrementedEvent),
                typeof(QtsUpdatedEvent),
                typeof(PtsForAuthKeyIdUpdatedEvent),
                typeof(QtsForAuthKeyIdUpdatedEvent),
                typeof(PtsGlobalSeqNoUpdatedEvent)
                );

            options.AddSnapshots(typeof(PtsSnapshot));


            options.UseMongoDbEventStore();
            options.UseMongoDbSnapshotStore();

            options.UseMongoDbReadModel<UpdatesAggregate, UpdatesId, UpdatesReadModel, PushReadModelMongoDbContext>();
            options.UseMongoDbReadModel<RpcResultAggregate, RpcResultId, RpcResultReadModel, PushReadModelMongoDbContext>();
            options.UseMongoDbReadModel<PtsAggregate, PtsId, PtsReadModel, PushReadModelMongoDbContext>();
            options.UseMongoDbReadModel<PtsAggregate, PtsId, PtsForAuthKeyIdReadModel, PushReadModelMongoDbContext>();
             
            configure?.Invoke(options);
        });

        services.AddMyTelegramCoreServices();
        services.AddMyTelegramHandlerServices();
        services.AddMyTelegramMessengerServices();
        services.AddMyEventFlow();
        services.AddMyTelegramIdGeneratorServices();

        // services.AddSingleton(typeof(IQueuedCommandExecutor<,,>), typeof(QueuedCommandExecutor<,,>));

        services.AddSystemTextJson(options =>
        {
            //options.AddSingleValueObjects();
            options.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
            options.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
        });

        services.AddCacheJsonSerializer(jsonOptions =>
        {
            jsonOptions.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
            jsonOptions.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
        });

        services.AddTransient<IQueryOnlyReadModelDescriptionProvider, QueryOnlyReadModelDescriptionProvider>();
        services.AddTransient<IQueryOnlyReadModelStore<IAccessHashReadModel>, MongoDbQueryOnlyReadModelStore<IAccessHashReadModel>>();
        BsonSerializer.RegisterSerializer(typeof(IAccessHashReadModel), new ImpliedImplementationInterfaceSerializer<IAccessHashReadModel, MyTelegram.ReadModel.MongoDB.AccessHashReadModel>());

    }
}