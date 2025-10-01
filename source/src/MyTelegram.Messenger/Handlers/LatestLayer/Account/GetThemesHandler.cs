namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Get installed themes
/// See <a href="https://corefork.telegram.org/method/account.getThemes" />
///</summary>
internal sealed class GetThemesHandler(IQueryProcessor queryProcessor) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetThemes, MyTelegram.Schema.Account.IThemes>
{
    protected override async Task<MyTelegram.Schema.Account.IThemes> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetThemes obj)
    {
        var themes = new List<ITheme>
        {
            new TTheme
            {
                Id = 20001,
                Slug = "mtg-day",
                Title = "MyTelegram Day",
                Creator = false,
                Default = true,
                Dark = false,
                BaseTheme = new TBaseThemeDay(),
                Settings = new TThemeSettings
                {
                    OutboxAccentColor = 0xFF2196F3,
                    MessageColors = new TVector<int>(new[]{0xFFFFFFFF, 0xFFF5F5F5})
                }
            },
            new TTheme
            {
                Id = 20002,
                Slug = "mtg-night",
                Title = "MyTelegram Night",
                Creator = false,
                Default = false,
                Dark = true,
                BaseTheme = new TBaseThemeNight(),
                Settings = new TThemeSettings
                {
                    OutboxAccentColor = 0xFF90CAF9,
                    MessageColors = new TVector<int>(new[]{0xFF1E1E1E, 0xFF121212})
                }
            }
        };

        var cfg = await queryProcessor.ProcessAsync(new GetUserConfigByKeyQuery(input.UserId, ((int)UserConfigType.Theme).ToString()));
        if (cfg != null && long.TryParse(cfg.Value, out var selectedId))
        {
            foreach (var t in themes)
            {
                if (t is TTheme theme)
                {
                    theme.Default = theme.Id == selectedId;
                }
            }
        }

        var r = new TThemes { Themes = new TVector<ITheme>(themes), Hash = obj.Hash };
        return r;
    }
}
