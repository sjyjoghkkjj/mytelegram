namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Download a <a href="https://corefork.telegram.org/cdn">CDN</a> file.
/// See <a href="https://corefork.telegram.org/method/upload.getCdnFile" />
///</summary>
internal sealed class GetCdnFileHandler(IFileStorage storage) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestGetCdnFile, MyTelegram.Schema.Upload.ICdnFile>
{
    protected override async Task<MyTelegram.Schema.Upload.ICdnFile> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestGetCdnFile obj)
    {
        var limit = Math.Clamp(obj.Limit, 1, 1024 * 1024);
        var (ok, slice) = await storage.GetSliceAsync(obj.FileToken.ReadInt64(), obj.Offset, limit);
        if (!ok)
        {
            return new MyTelegram.Schema.Upload.TCdnFileReuploadNeeded { RequestToken = obj.FileToken };
        }
        return new MyTelegram.Schema.Upload.TCdnFile { Bytes = slice };
    }
}
