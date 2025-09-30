namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Returns content of a whole file or its part.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 FILE_REFERENCE_* The file reference expired, it <a href="https://corefork.telegram.org/api/file_reference">must be refreshed</a>.
/// 406 FILEREF_UPGRADE_NEEDED The client has to be updated in order to support <a href="https://corefork.telegram.org/api/file_reference">file references</a>.
/// 400 FILE_ID_INVALID The provided file id is invalid.
/// 400 FILE_REFERENCE_EXPIRED File reference expired, it must be refetched as described in <a href="https://corefork.telegram.org/api/file_reference">the documentation</a>.
/// 400 LIMIT_INVALID The provided limit is invalid.
/// 400 LOCATION_INVALID The provided location is invalid.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 OFFSET_INVALID The provided offset is invalid.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/upload.getFile" />
///</summary>
internal sealed class GetFileHandler(IInMemoryFileStorage storage) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestGetFile, MyTelegram.Schema.Upload.IFile>
{
    protected override async Task<MyTelegram.Schema.Upload.IFile> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestGetFile obj)
    {
        var limit = Math.Clamp(obj.Limit, 1, 1024 * 1024);
        int offset;
        switch (obj.Location)
        {
            case MyTelegram.Schema.TInputFileLocation loc:
                offset = obj.Offset;
                var (ok, slice) = await storage.GetSliceAsync(loc.VolumeId, offset, limit);
                if (!ok)
                {
                    RpcErrors.RpcErrors400.LocationInvalid.ThrowRpcError();
                }
                return new MyTelegram.Schema.Upload.TFile { Type = new MyTelegram.Schema.TStorageFileJpeg(), Bytes = slice };
            default:
                RpcErrors.RpcErrors400.LocationInvalid.ThrowRpcError();
                break;
        }

        throw new InvalidOperationException();
    }
}
