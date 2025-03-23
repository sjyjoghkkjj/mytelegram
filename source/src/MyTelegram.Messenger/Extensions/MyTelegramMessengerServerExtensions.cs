using EventFlow.Core.Caching;
using EventFlow.MongoDB.Extensions;
using EventFlow.ReadStores;
using MyTelegram.Converters.Extensions;
using MyTelegram.Messenger.NativeAot;
using MyTelegram.Messenger.Services.Impl;
using MyTelegram.QueryHandlers.MongoDB;
using MyTelegram.ReadModel.MongoDB;
using MyTelegram.ReadModel.ReadModelLocators;
using MyTelegram.Services.Services.IdGenerator;
using ChannelFullReadModel = MyTelegram.ReadModel.MongoDB.ChannelFullReadModel;
using ChannelReadModel = MyTelegram.ReadModel.MongoDB.ChannelReadModel;
using PhotoReadModel = MyTelegram.ReadModel.MongoDB.PhotoReadModel;
using PtsForAuthKeyIdReadModel = MyTelegram.ReadModel.MongoDB.PtsForAuthKeyIdReadModel;
using PtsReadModel = MyTelegram.ReadModel.MongoDB.PtsReadModel;
using UserReadModel = MyTelegram.ReadModel.MongoDB.UserReadModel;

namespace MyTelegram.Messenger.Extensions;

public static class MyTelegramMessengerServerExtensions
{
    public static void AddMyTelegramMessengerServer(this IServiceCollection services,
       Action<IEventFlowOptions>? configure = null)
    {
        services.AddTransient<IMongoDbIndexesCreator, MongoDbIndexesCreator>();
        services.AddTransient<IChatInviteLinkHelper, ChatInviteLinkHelper>();

        services.AddEventFlow(options =>
        {
            options.AddDefaults(typeof(MyTelegramMessengerServerExtensions).Assembly);
            options.AddDefaults(typeof(EventFlowExtensions).Assembly);
            options.Configure(c => { c.IsAsynchronousSubscribersEnabled = true; });

            options.UseMongoDbEventStore();
            options.UseMongoDbSnapshotStore();

            options.AddMessengerMongoDbReadModel();
            options.AddMongoDbQueryHandlers();

            options.AddSystemTextJson(jsonSerializerOptions =>
            {
                jsonSerializerOptions.AddSingleValueObjects(
                    new EventFlow.SystemTextJsonSingleValueObjectConverter<CacheKey>());
                jsonSerializerOptions.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
            });
            configure?.Invoke(options);
        })
            .AddMyTelegramCoreServices()
            .AddMyTelegramHandlerServices()
            .AddMyTelegramMessengerServices()
            .AddMyTelegramIdGeneratorServices()
            .AddMyEventFlow()
            ;

        services.AddMyNativeAot();
    }

    public static IServiceCollection AddMyTelegramIdGeneratorServices(this IServiceCollection services)
    {
        services.AddSingleton<IHiLoValueGeneratorCache, HiLoValueGeneratorCache>();
        services.AddTransient<IHiLoValueGeneratorFactory, HiLoValueGeneratorFactory>();
        services.AddSingleton<IHiLoStateBlockSizeHelper, HiLoStateBlockSizeHelper>();
        services.AddSingleton<IRedisIdGenerator, RedisIdGenerator>();
        services.AddTransient<IHiLoHighValueGenerator, MongoDbHighValueGenerator>();
        services.AddSingleton<IMongoDbIdGenerator, MongoDbIdGenerator>();

        return services;
    }

    public static IServiceCollection AddMyTelegramMessengerServices(this IServiceCollection services)
    {
        services.RegisterMongoDbSerializer();
        services.RegisterServices();

        //services.AddLayeredServices();
        //services.AddLayeredResultConverters();
        //services.AddRequestConverters();
        services.AddMyTelegramIdGeneratorServices();
        services.RegisterHandlers(typeof(MyTelegramMessengerServerExtensions).Assembly);
        //services.RegisterAllMappers();
        services.AddMyTelegramConverters();

        services.AddSingleton(typeof(ICacheHelper<,>), typeof(CacheHelper<,>));
        services.AddSingleton(typeof(IReadModelCacheHelper<>), typeof(ReadModelCacheHelper<>));
        services.AddSingleton<IReadModelDomainEventApplier, MyReadModelDomainEventApplier>();

        services.AddTransient<ICachedReadModelManager, MultipleAggregateCachedReadModelManager<IUserReadModel, UserReadModel, IUserReadModelLocator>>();
        services.AddTransient<ICachedReadModelManager, MultipleAggregateCachedReadModelManager<IChannelReadModel, ChannelReadModel, IChannelReadModelLocator>>();
        services.AddTransient<ICachedReadModelManager, SingleAggregateCachedReadModelManager<IPhotoReadModel, PhotoReadModel>>();
        services.AddTransient<ICachedReadModelManager, MultipleAggregateCachedReadModelManager<IChannelFullReadModel, ChannelFullReadModel, IChannelFullReadModelLocator>>();
        services.AddTransient<ICachedReadModelManager, SingleAggregateCachedReadModelManager<IPtsReadModel, PtsReadModel>>();
        services.AddTransient<ICachedReadModelManager, SingleAggregateCachedReadModelManager<IPtsForAuthKeyIdReadModel, PtsForAuthKeyIdReadModel>>();

        return services;
    }

    public static void AddMyMongoDbReadModelServices(this IEventFlowOptions options)
    {

    }
}