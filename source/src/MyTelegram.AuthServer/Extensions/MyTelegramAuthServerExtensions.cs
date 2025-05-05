namespace MyTelegram.AuthServer.Extensions;

public static class MyTelegramAuthServerExtensions
{
    public static IServiceCollection AddAuthServer(this IServiceCollection services)
    {
        services.RegisterServices();

        services.AddMyTelegramHandlerServices();

        return services;
    }

    public static void ConfigureEventBus(this IEventBus eventBus)
    {
        eventBus.Subscribe<UnencryptedMessage, UnencryptedMessageHandler>();
    }
}