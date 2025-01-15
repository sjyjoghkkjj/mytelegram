// ReSharper disable All

namespace MyTelegram.Handlers.Help;

/// <summary>
///     Get app-specific configuration, see
///     <a href="https://corefork.telegram.org/api/config#client-configuration">client configuration</a> for more info on
///     the result.
///     See <a href="https://corefork.telegram.org/method/help.getAppConfig" />
/// </summary>
internal sealed class GetAppConfigHandler (IAppConfigHelper appConfigHelper):
    RpcResultObjectHandler<Schema.Help.RequestGetAppConfig, Schema.Help.IAppConfig>,
    Help.IGetAppConfigHandler
{
    protected override Task<Schema.Help.IAppConfig> HandleCoreAsync(IRequestInput input,
        Schema.Help.RequestGetAppConfig obj)
    {
        var config = appConfigHelper.GetAppConfig();

        var appConfig = new TAppConfig
        {
            Config = config
        };

        return Task.FromResult<IAppConfig>(appConfig);
    }
}