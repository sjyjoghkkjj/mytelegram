namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Get theme information
/// <para>Possible errors</para>
/// Code Type Description
/// 400 THEME_FORMAT_INVALID Invalid theme format provided.
/// 400 THEME_INVALID Invalid theme provided.
/// See <a href="https://corefork.telegram.org/method/account.getTheme" />
///</summary>
internal sealed class GetThemeHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetTheme, MyTelegram.Schema.ITheme>
{
    protected override Task<MyTelegram.Schema.ITheme> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetTheme obj)
    {
        long id = 0;
        string slug = string.Empty;
        switch (obj.Theme)
        {
            case TInputTheme it:
                id = it.Id; break;
            case TInputThemeSlug s:
                slug = s.Slug;
                id = s.Slug switch { "mtg-day" => 20001, "mtg-night" => 20002, _ => 0 };
                break;
        }

        if (id == 0)
        {
            RpcErrors.RpcErrors400.ThemeInvalid.ThrowRpcError();
        }

        ITheme result = (slug == "mtg-night" || id == 20002)
            ? new TTheme
            {
                Id = 20002,
                Slug = "mtg-night",
                Title = "MyTelegram Night",
                Default = false,
                Dark = true,
                BaseTheme = new TBaseThemeNight(),
                Settings = new TThemeSettings
                {
                    OutboxAccentColor = 0xFF90CAF9,
                    MessageColors = new TVector<int>(new[]{0xFF1E1E1E, 0xFF121212})
                }
            }
            : new TTheme
            {
                Id = 20001,
                Slug = "mtg-day",
                Title = "MyTelegram Day",
                Default = true,
                Dark = false,
                BaseTheme = new TBaseThemeDay(),
                Settings = new TThemeSettings
                {
                    OutboxAccentColor = 0xFF2196F3,
                    MessageColors = new TVector<int>(new[]{0xFFFFFFFF, 0xFFF5F5F5})
                }
            };

        return Task.FromResult(result);
    }
}
