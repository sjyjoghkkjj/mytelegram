using EventFlow.MongoDB.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyTelegram;
using MyTelegram.Caching.Redis;
using MyTelegram.Domain.Aggregates.Device;
using MyTelegram.Domain.Aggregates.Pts;
using MyTelegram.Messenger;
using MyTelegram.Messenger.CommandServer.BackgroundServices;
using MyTelegram.Messenger.CommandServer.Extensions;
using MyTelegram.Messenger.NativeAot;
using MyTelegram.Services.NativeAot;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Console.Title = $"MyTelegram messenger command server (layer {MyTelegramServerDomainConsts.Layer})";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.Console(theme: AnsiConsoleTheme.Code))
    .WriteTo.Async(c => c.File("Logs/startup-log.txt"))
    .CreateLogger();

Log.Information("{Info} {Version}", "MyTelegram messenger command server", typeof(Program).Assembly.GetName().Version);
Log.Information("{Description} {Url}",
    "For more information, please visit",
    MyTelegramServerDomainConsts.RepositoryUrl);

Log.Information("MyTelegram messenger command server(API layer={Layer}) starting...",
    MyTelegramServerDomainConsts.Layer);

AppDomain.CurrentDomain.UnhandledException += (_,
    e) =>
{
    Log.Error(e.ExceptionObject.ToString() ?? string.Empty);
};
TaskScheduler.UnobservedTaskException += (_,
    e) =>
{
    Log.Error(e.Exception.ToString());
};
var builder = Host.CreateDefaultBuilder(args);
//builder.UseAutofac();
builder.UseSerilog((context,
    configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.ConfigureHostOptions(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

builder.ConfigureAppConfiguration(options =>
{
    options.AddEnvironmentVariables();
    options.AddCommandLine(args);
});
builder.ConfigureServices((ctx,
    services) =>
{
    services.AddOptions<MyTelegramMessengerServerOptions>()
        .Bind(ctx.Configuration.GetRequiredSection("App"))
        .ValidateDataAnnotations()
        .ValidateOnStart()
        ;
    services.Configure<EventBusRabbitMqOptions>(ctx.Configuration.GetRequiredSection("RabbitMQ:EventBus"));
    services.Configure<RabbitMqOptions>(ctx.Configuration.GetRequiredSection("RabbitMQ:Connections:Default"));

    //services.AddMyTelegramRabbitMqEventBus();

    var eventBusOptions = ctx.Configuration.GetRequiredSection("RabbitMQ:EventBus").Get<EventBusRabbitMqOptions>();
    var rabbitMqOptions = ctx.Configuration.GetRequiredSection("RabbitMQ:Connections:Default").Get<RabbitMqOptions>();

    services.AddRebusEventBus(options =>
    {
        options.Transport(t =>
        {
            t.UseRabbitMq(
                    $"amqp://{rabbitMqOptions!.UserName}:{rabbitMqOptions.Password}@{rabbitMqOptions.HostName}:{rabbitMqOptions.Port}",
                    eventBusOptions!.ClientName)
                .ExchangeNames(eventBusOptions.ExchangeName, eventBusOptions.TopicExchangeName ?? "RebusTopics")
                ;
        });
        options.AddSystemTextJson(jsonOptions =>
        {
            jsonOptions.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
            jsonOptions.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
        });
    });

    services.AddMyTelegramMessengerServer(options =>
    {
        options.AddDefaults(Assembly.GetEntryAssembly());
        options.ConfigureMongoDb(ctx.Configuration.GetConnectionString("Default"),
            ctx.Configuration["App:DatabaseName"]);
    });

    services.AddMyTelegramStackExchangeRedisCache(options =>
    {
        options.Configuration = ctx.Configuration.GetValue<string>("Redis:Configuration");
    });
    services.AddCacheJsonSerializer(options =>
    {
        options.TypeInfoResolverChain.Add(MyJsonSerializeContext.Default);
        options.TypeInfoResolverChain.Add(MyMessengerJsonContext.Default);
    });

    services.AddMyTelegramMessengerCommandServer();
    services.AddHostedService<MyTelegramCommandServerBackgroundService>();
    services.AddHostedService<MyTelegramInvokeAfterMsgProcessorBackgroundService>();

    services.AddHostedService<MessageQueueDataProcessorBackgroundService<MessengerCommandDataReceivedEvent>>();
    services.AddHostedService<MessageQueueDataProcessorBackgroundService<NewDeviceCreatedEvent>>();
    services.AddHostedService<MessageQueueDataProcessorBackgroundService<ISessionMessage>>();
    services.AddHostedService<MessageQueueDataProcessorBackgroundService<IDomainEvent>>();
    services.AddHostedService<QueuedCommandExecutorBackgroundService<DeviceAggregate, DeviceId>>();
    services.AddHostedService<QueuedCommandExecutorBackgroundService<PtsAggregate, PtsId>>();

    services.Configure<HostOptions>(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });
});


var app = builder.Build();
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.ConfigureEventBus();

await app.RunAsync();
