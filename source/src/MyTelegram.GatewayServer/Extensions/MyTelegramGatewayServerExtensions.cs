using MyTelegram.MTProto.Extensions;

namespace MyTelegram.GatewayServer.Extensions;

public static class MyTelegramGatewayServerExtensions
{
    public static void AddMyTelegramGatewayServer(this IServiceCollection services)
    {
        services.RegisterServices(typeof(MyTelegramGatewayServerExtensions).Assembly);
        services.AddMyTelegramMtProto();
        services.AddMyTelegramCoreServices();
    }

    public static void ConfigureEventBus(this IEventBus eventBus)
    {
        eventBus.Subscribe<EncryptedMessageResponse, EncryptedMessageResponseEventHandler>();
        eventBus.Subscribe<UnencryptedMessageResponse, UnencryptedMessageResponseEventHandler>();
        eventBus.Subscribe<AuthKeyNotFoundEvent, AuthKeyNotFoundEventHandler>();
        eventBus.Subscribe<TransportErrorEvent, TransportErrorEventHandler>();
        eventBus.Subscribe<PingTimeoutEvent, PingTimeoutEventHandler>();
    }
}