namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Download a <a href="https://corefork.telegram.org/cdn">CDN</a> file.
/// See <a href="https://corefork.telegram.org/method/upload.getCdnFile" />
///</summary>
internal sealed class GetCdnFileHandler(IFileStorage storage, IDataCenterHelper dcHelper, ICdnTokenService cdnTokens, IAesHelper aesHelper) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestGetCdnFile, MyTelegram.Schema.Upload.ICdnFile>
{
    protected override async Task<MyTelegram.Schema.Upload.ICdnFile> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestGetCdnFile obj)
    {
        // Must be called on CDN DC
        if (!dcHelper.IsCdnDc(dcHelper.GetThisDcId()))
        {
            RpcErrors.RpcErrors400.CdnMethodInvalid.ThrowRpcError();
        }
        var limit = Math.Clamp(obj.Limit, 1, 1024 * 1024);
        if (!cdnTokens.TryResolveFileId(obj.FileToken, out var fileId))
        {
            RpcErrors.RpcErrors400.FileTokenInvalid.ThrowRpcError();
        }
        var (ok, slice) = await storage.GetSliceAsync(fileId, obj.Offset, limit);
        if (!ok)
        {
            // Generate request_token signed by CDN DC to ask master DC to reupload
            var thisCdnDc = dcHelper.GetThisDcId();
            var requestToken = rsaKeys.Sign(thisCdnDc, obj.FileToken);
            return new MyTelegram.Schema.Upload.TCdnFileReuploadNeeded { RequestToken = requestToken };
        }
        if (!cdnTokens.TryGetEncryption(obj.FileToken, out var key, out var iv))
        {
            RpcErrors.RpcErrors400.FileTokenInvalid.ThrowRpcError();
        }
        // Encrypt slice with AES-CTR using offset
        var buff = slice.ToArray();
        var mem = new Memory<byte>(buff);
        aesHelper.Ctr256Encrypt(mem, key, iv, obj.Offset);
        return new MyTelegram.Schema.Upload.TCdnFile { Bytes = buff };
    }
}
