namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Get info about a certain <a href="https://corefork.telegram.org/api/wallpapers">wallpaper</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 WALLPAPER_INVALID The specified wallpaper is invalid.
/// See <a href="https://corefork.telegram.org/method/account.getWallPaper" />
///</summary>
internal sealed class GetWallPaperHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetWallPaper, MyTelegram.Schema.IWallPaper>
{
    protected override Task<MyTelegram.Schema.IWallPaper> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetWallPaper obj)
    {
        long id = 0;
        string slug = string.Empty;
        switch (obj.Wallpaper)
        {
            case TInputWallPaper i:
                id = i.Id; break;
            case TInputWallPaperSlug s:
                slug = s.Slug;
                id = s.Slug switch { "mtg-blue" => 10001, "mtg-dark" => 10002, _ => 0 };
                break;
            case TInputWallPaperNoFile:
                id = 10001; slug = "mtg-blue"; break;
        }

        if (id == 0)
        {
            RpcErrors.RpcErrors400.WallpaperInvalid.ThrowRpcError();
        }

        // For now, return NoFile entry with settings; integration with real Document can be added later
        IWallPaper result = slug == "mtg-dark"
            ? new TWallPaperNoFile
            {
                Id = id,
                Slug = slug,
                Dark = true,
                Settings = new TWallPaperSettings { BackgroundColor = 0xFF121212 }
            }
            : new TWallPaperNoFile
            {
                Id = id,
                Slug = string.IsNullOrEmpty(slug) ? "mtg-blue" : slug,
                Dark = false,
                Settings = new TWallPaperSettings { BackgroundColor = 0xFF1E88E5 }
            };

        return Task.FromResult(result);
    }
}
