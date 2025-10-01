namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Save a theme
/// See <a href="https://corefork.telegram.org/method/account.saveTheme" />
///</summary>
internal sealed class SaveThemeHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestSaveTheme, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestSaveTheme obj)
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
        var value = obj.Unsave ? string.Empty : themeId.ToString();
        await commandBus.PublishAsync(new UpdateUserConfigCommand(
            UserConfigId.Create(input.UserId, key), input.ToRequestInfo(),
            input.UserId, key, value));

        return new TBoolTrue();
    }
}
