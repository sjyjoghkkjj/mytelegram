namespace MyTelegram.Messenger.Handlers.LatestLayer.Account;

///<summary>
/// Install/uninstall <a href="https://corefork.telegram.org/api/wallpapers">wallpaper</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 WALLPAPER_INVALID The specified wallpaper is invalid.
/// See <a href="https://corefork.telegram.org/method/account.saveWallPaper" />
///</summary>
internal sealed class SaveWallPaperHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestSaveWallPaper, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestSaveWallPaper obj)
    {
        long wallPaperId = 0;
        switch (obj.Wallpaper)
        {
            case TInputWallPaperSlug slug:
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
        var value = obj.Unsave ? string.Empty : wallPaperId.ToString();
        await commandBus.PublishAsync(new UpdateUserConfigCommand(
            UserConfigId.Create(input.UserId, key), input.ToRequestInfo(),
            input.UserId, key, value));

        return new TBoolTrue();
    }
}
