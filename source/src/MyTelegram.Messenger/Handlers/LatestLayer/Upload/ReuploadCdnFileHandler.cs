namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Request a reupload of a certain file to a <a href="https://corefork.telegram.org/cdn">CDN DC</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 500 CDN_UPLOAD_TIMEOUT A server-side timeout occurred while reuploading the file to the CDN DC.
/// 400 FILE_TOKEN_INVALID The specified file token is invalid.
/// 400 RSA_DECRYPT_FAILED Internal RSA decryption failed.
/// See <a href="https://corefork.telegram.org/method/upload.reuploadCdnFile" />
///</summary>
internal sealed class ReuploadCdnFileHandler(IDataCenterHelper dcHelper, ICdnTokenService cdnTokens, ICdnRsaKeyService rsaKeys) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestReuploadCdnFile, TVector<MyTelegram.Schema.IFileHash>>
{
    protected override Task<TVector<MyTelegram.Schema.IFileHash>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestReuploadCdnFile obj)
    {
        // Must be called on master DC
        if (dcHelper.IsCdnDc(dcHelper.GetThisDcId()))
        {
            RpcErrors.RpcErrors400.CdnMethodInvalid.ThrowRpcError();
        }
        // Verify request_token signature (RSA) using CDN DC public key.
        // Here we don't know which CDN DC signed it; in real world, token encodes the DC ID. For demo assume first CDN DC.
        var cdnDc = dcHelper.GetFirstCdnDcId() ?? 0;
        if (cdnDc == 0)
        {
            RpcErrors.RpcErrors400.LocationInvalid.ThrowRpcError();
        }
        // Accept both RSA and HMAC for backwards dev compatibility
        var rsaOk = rsaKeys.Verify(cdnDc, obj.FileToken, obj.RequestToken);
        var hmacOk = cdnTokens.ValidateRequestToken(obj.FileToken, obj.RequestToken);
        if (!rsaOk && !hmacOk)
        {
            RpcErrors.RpcErrors400.RequestTokenInvalid.ThrowRpcError();
        }
        if (!cdnTokens.TryResolveFileId(obj.FileToken, out _))
        {
            RpcErrors.RpcErrors400.FileTokenInvalid.ThrowRpcError();
        }
        // Placeholder: return empty list (client may proceed)
        return Task.FromResult(new TVector<MyTelegram.Schema.IFileHash>());
    }
}
