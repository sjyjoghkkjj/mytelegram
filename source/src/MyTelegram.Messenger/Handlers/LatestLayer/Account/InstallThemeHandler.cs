namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Install a theme
/// See <a href="https://corefork.telegram.org/method/account.installTheme" />
///</summary>
internal sealed class InstallThemeHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestInstallTheme, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestInstallTheme obj)
    {
        long themeId = 0;
        switch (obj.Theme)
        {
            case TInputThemeSlug slug:
                themeId = slug.Slug switch
                {
                    "mtg-day" => 20001,
                    "mtg-night" => 20002,
                    _ => 0
                };
                break;
            case TInputTheme inputTheme:
                themeId = inputTheme.Id;
                break;
        }

        if (themeId == 0)
        {
            RpcErrors.RpcErrors400.ThemeInvalid.ThrowRpcError();
        }

        var key = ((int)UserConfigType.Theme).ToString();
        await commandBus.PublishAsync(new UpdateUserConfigCommand(
            UserConfigId.Create(input.UserId, key), input.ToRequestInfo(),
            input.UserId, key, themeId.ToString()));

        return new TBoolTrue();
    }
}
