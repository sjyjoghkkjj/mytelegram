using MyTelegram.Domain.Extensions;
using MyTelegram.QueryHandlers.MongoDB.User;

namespace MyTelegram.DataSeeder.Extensions;

public static class MyTelegramDataSeederExtensions
{
    public static void AddMyTelegramDataSeeder(this IServiceCollection services,
        Action<IEventFlowOptions>? configure = null)
    {
        services.RegisterMongoDbSerializer();
        services.AddMyEventFlow();
        services.AddEventFlow(options =>
        {
            options.AddDefaults(typeof(EventFlowExtensions).Assembly);
            options.Configure(c => { c.IsAsynchronousSubscribersEnabled = true; });
            options.UseMongoDbEventStore();
            options.AddMessengerMongoDbReadModel();
            options.AddMyMongoDbReadModel();
            options.AddQueryHandlers(typeof(GetUserByIdQueryHandler));
            options.AddSystemTextJson(jsonSerializerOptions =>
            {
                //jsonSerializerOptions.AddSingleValueObjects(
                //    new SystemTextJsonSingleValueObjectConverter<CacheKey>());
                jsonSerializerOptions.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
                jsonSerializerOptions.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
            });
            configure?.Invoke(options);
        });

        services.AddTransient(typeof(IDataSeeder<>), typeof(DataSeeder<>));
        services.RegisterServices();
    }
}