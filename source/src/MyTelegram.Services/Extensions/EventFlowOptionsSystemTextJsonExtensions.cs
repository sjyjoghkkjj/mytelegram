using EventFlow;
using Microsoft.Extensions.DependencyInjection;
using MyTelegram.Services.NativeAot;
using System.Text.Json;

namespace MyTelegram.Services.Extensions;

public static class EventFlowOptionsSystemTextJsonExtensions
{
    public static IEventFlowOptions AddSystemTextJson(
        this IEventFlowOptions eventFlowOptions,
        Action<JsonSerializerOptions>? configure = null)
    {
        eventFlowOptions.ServiceCollection.AddSystemTextJson(configure);

        return eventFlowOptions;
    }

    public static IServiceCollection AddSystemTextJson(this IServiceCollection services,
        Action<JsonSerializerOptions>? configure = null)
    {
        services.AddMySystemTextJson(options =>
        {
            options.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
            configure?.Invoke(options);
        });

        return services;
    }
}