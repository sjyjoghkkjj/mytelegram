namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Saves a part of a large file (over 10 MB in size) to be later passed to one of the methods.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 FILE_PARTS_INVALID The number of file parts is invalid.
/// 400 FILE_PART_EMPTY The provided file part is empty.
/// 400 FILE_PART_INVALID The file part number is invalid.
/// 400 FILE_PART_SIZE_CHANGED Provided file part size has changed.
/// 400 FILE_PART_SIZE_INVALID The provided file part size is invalid.
/// 400 FILE_PART_TOO_BIG The uploaded file part is too big.
/// See <a href="https://corefork.telegram.org/method/upload.saveBigFilePart" />
///</summary>
internal sealed class SaveBigFilePartHandler(IFileStorage storage) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestSaveBigFilePart, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestSaveBigFilePart obj)
    {
        if (obj.FilePart < 0 || obj.FileTotalParts <= 0 || obj.Bytes.IsEmpty)
        {
            RpcErrors.RpcErrors400.FilePartInvalid.ThrowRpcError();
        }
        var ok = await storage.SavePartAsync(obj.FileId, obj.FilePart, obj.FileTotalParts, obj.Bytes, isBig: true);
        if (!ok)
        {
            RpcErrors.RpcErrors400.FilePartInvalid.ThrowRpcError();
        }
        return new TBoolTrue();
    }
}
