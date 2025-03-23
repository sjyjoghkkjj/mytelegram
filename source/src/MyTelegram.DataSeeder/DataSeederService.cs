namespace MyTelegram.DataSeeder;

public class DataSeederService(
    ILogger<DataSeederService> logger,
    IDataSeederHelper dataSeederHelper,
    IUserDataSeeder userDataSeeder
) : IDataSeederService, ITransientDependency
{
    public async Task SeedAllAsync()
    {
        try
        {
            var config = await dataSeederHelper.LoadDataSeederConfigAsync();

            // Users
            if (!config.IsUserCreated)
            {
                await userDataSeeder.SeedAsync();
                config.IsUserCreated = true;
            }
        }
        finally
        {
            await dataSeederHelper.SaveDataSeederConfigAsync();
        }

        logger.LogInformation("All data created");
    }
}