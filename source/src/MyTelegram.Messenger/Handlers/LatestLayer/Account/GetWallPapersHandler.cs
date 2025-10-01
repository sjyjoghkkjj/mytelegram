namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Returns a list of available <a href="https://corefork.telegram.org/api/wallpapers">wallpapers</a>.
/// See <a href="https://corefork.telegram.org/method/account.getWallPapers" />
///</summary>
internal sealed class GetWallPapersHandler(IQueryProcessor queryProcessor) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetWallPapers, MyTelegram.Schema.Account.IWallPapers>
{
    protected override async Task<MyTelegram.Schema.Account.IWallPapers> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetWallPapers obj)
    {
        // Minimal static catalog, file-less entries with slugs; client may request concrete by slug later
        var wallpapers = new List<IWallPaper>
        {
            new TWallPaperNoFile
            {
                Id = 10001,
                Slug = "mtg-blue",
                Default = true,
                Dark = false,
                Settings = new TWallPaperSettings
                {
                    Blur = false,
                    Motion = false,
                    BackgroundColor = 0xFF1E88E5
                }
            },
            new TWallPaperNoFile
            {
                Id = 10002,
                Slug = "mtg-dark",
                Default = false,
                Dark = true,
                Settings = new TWallPaperSettings
                {
                    Blur = false,
                    Motion = false,
                    BackgroundColor = 0xFF121212
                }
            }
        };

        // Read installed wallpaper from user config
        var cfg = await queryProcessor.ProcessAsync(new GetUserConfigByKeyQuery(input.UserId, ((int)UserConfigType.WallPaper).ToString()));
        if (cfg != null && long.TryParse(cfg.Value, out var selectedId))
        {
            foreach (var w in wallpapers)
            {
                if (w is TWallPaperNoFile nf)
                {
                    nf.Default = nf.Id == selectedId;
                }
            }
        }

        return new TWallPapers
        {
            Wallpapers = new TVector<IWallPaper>(wallpapers)
        };
    }
}
