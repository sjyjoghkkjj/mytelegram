namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Get SHA256 hashes for verifying downloaded <a href="https://corefork.telegram.org/cdn">CDN</a> files
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CDN_METHOD_INVALID You can't call this method in a CDN DC.
/// 400 RSA_DECRYPT_FAILED Internal RSA decryption failed.
/// See <a href="https://corefork.telegram.org/method/upload.getCdnFileHashes" />
///</summary>
internal sealed class GetCdnFileHashesHandler(IFileStorage storage, IDataCenterHelper dcHelper) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestGetCdnFileHashes, TVector<MyTelegram.Schema.IFileHash>>
{
    protected override Task<TVector<MyTelegram.Schema.IFileHash>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestGetCdnFileHashes obj)
    {
        if (!dcHelper.IsCdnDc(dcHelper.GetThisDcId()))
        {
            RpcErrors.RpcErrors400.CdnMethodInvalid.ThrowRpcError();
        }
        var tokenId = obj.FileToken.ReadInt64();
        var hashes = ComputeHashes(tokenId, obj.Offset, 131072); // 128 KB chunk size like Telegram CDN
        return Task.FromResult(new TVector<MyTelegram.Schema.IFileHash>(hashes));
    }

    private IEnumerable<MyTelegram.Schema.IFileHash> ComputeHashes(long fileId, long offset, int chunkSize)
    {
        // Read chunks starting at offset, return up to a reasonable number per call (e.g., 64 chunks)
        const int maxChunks = 64;
        var hashes = new List<MyTelegram.Schema.IFileHash>();
        for (int i = 0; i < maxChunks; i++)
        {
            var off = checked((int)(offset + (long)i * chunkSize));
            var (ok, slice) = storage.GetSliceAsync(fileId, off, chunkSize).GetAwaiter().GetResult();
            if (!ok || slice.IsEmpty)
            {
                break;
            }
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(slice.Span.ToArray());
            hashes.Add(new MyTelegram.Schema.TFileHash { Offset = off, Limit = slice.Length, Hash = hash });
            if (slice.Length < chunkSize)
            {
                break; // EOF
            }
        }
        return hashes;
    }
}
