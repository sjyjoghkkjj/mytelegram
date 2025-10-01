namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Get SHA256 hashes for verifying downloaded files
/// <para>Possible errors</para>
/// Code Type Description
/// 400 LOCATION_INVALID The provided location is invalid.
/// See <a href="https://corefork.telegram.org/method/upload.getFileHashes" />
///</summary>
internal sealed class GetFileHashesHandler(IFileStorage storage) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestGetFileHashes, TVector<MyTelegram.Schema.IFileHash>>
{
    protected override Task<TVector<MyTelegram.Schema.IFileHash>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestGetFileHashes obj)
    {
        var (ok, fileId) = Resolve(obj.Location);
        if (!ok)
        {
            RpcErrors.RpcErrors400.LocationInvalid.ThrowRpcError();
        }
        var hashes = ComputeHashes(fileId, obj.Offset, 131072);
        return Task.FromResult(new TVector<MyTelegram.Schema.IFileHash>(hashes));
    }

    private static (bool ok, long fileId) Resolve(MyTelegram.Schema.IInputFileLocation loc) => loc switch
    {
        MyTelegram.Schema.TInputFileLocation f => (true, f.VolumeId),
        MyTelegram.Schema.TInputPhotoFileLocation p => (true, p.Id),
        MyTelegram.Schema.TInputDocumentFileLocation d => (true, d.Id),
        MyTelegram.Schema.TInputEncryptedFileLocation e => (true, e.Id),
        _ => (false, 0)
    };

    private IEnumerable<MyTelegram.Schema.IFileHash> ComputeHashes(long fileId, long offset, int chunkSize)
    {
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
                break;
            }
        }
        return hashes;
    }
}
