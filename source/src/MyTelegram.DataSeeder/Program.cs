Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.Console(theme: AnsiConsoleTheme.Code))
    .WriteTo.Async(c => c.File("Logs/startup-log.txt"))
    .CreateLogger();

Log.Information("{Info} {Version}", "MyTelegram Data Seeder", typeof(Program).Assembly.GetName().Version);
Log.Information("{Description} {Url}", "For more information, please visit", "https://github.com/loyldg/mytelegram");

Log.Information("MyTelegram data seeder starting...");

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureAppConfiguration(options =>
{
    options.AddEnvironmentVariables();
    options.AddCommandLine(args);
});

builder.UseSerilog((context,
    configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.ConfigureServices((context,
    services) =>
{
    services.Configure<MyTelegramDataSeederOptions>(context.Configuration.GetRequiredSection("App"));

    services.AddMyTelegramDataSeeder(options =>
    {
        options.ConfigureMongoDb(context.Configuration.GetConnectionString("Default"),
            context.Configuration["App:DatabaseName"]
        );
    });

    services.AddHostedService<MyTelegramDataSeederBackgroundService>();
});

var app = builder.Build();

await app.RunAsync();