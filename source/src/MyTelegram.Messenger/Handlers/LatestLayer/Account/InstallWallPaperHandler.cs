namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Install <a href="https://corefork.telegram.org/api/wallpapers">wallpaper</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 WALLPAPER_INVALID The specified wallpaper is invalid.
/// See <a href="https://corefork.telegram.org/method/account.installWallPaper" />
///</summary>
internal sealed class InstallWallPaperHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestInstallWallPaper, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestInstallWallPaper obj)
    {
        // Accept slug/no-file and persist selection in UserConfig
        long wallPaperId = 0;
        switch (obj.Wallpaper)
        {
            case TInputWallPaperSlug slug:
                // Map known slugs to static IDs
                wallPaperId = slug.Slug switch
                {
                    "mtg-blue" => 10001,
                    "mtg-dark" => 10002,
                    _ => 0
                };
                break;
            case TInputWallPaper inputWallPaper:
                wallPaperId = inputWallPaper.Id;
                break;
            case TInputWallPaperNoFile:
                wallPaperId = 10001;
                break;
        }

        if (wallPaperId == 0)
        {
            RpcErrors.RpcErrors400.WallpaperInvalid.ThrowRpcError();
        }

        var key = ((int)UserConfigType.WallPaper).ToString();
        await commandBus.PublishAsync(new UpdateUserConfigCommand(
            UserConfigId.Create(input.UserId, key), input.ToRequestInfo(),
            input.UserId, key, wallPaperId.ToString()));

        return new TBoolTrue();
    }
}
