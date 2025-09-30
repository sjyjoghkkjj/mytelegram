namespace MyTelegram.Messenger.Handlers.LatestLayer.Upload;

///<summary>
/// Saves a part of file for further sending to one of the methods.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 FILE_PART_EMPTY The provided file part is empty.
/// 400 FILE_PART_INVALID The file part number is invalid.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// See <a href="https://corefork.telegram.org/method/upload.saveFilePart" />
///</summary>
internal sealed class SaveFilePartHandler(IFileStorage storage) : RpcResultObjectHandler<MyTelegram.Schema.Upload.RequestSaveFilePart, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Upload.RequestSaveFilePart obj)
    {
        if (obj.FilePart < 0 || obj.FileTotalParts <= 0 || obj.Bytes.IsEmpty)
        {
            RpcErrors.RpcErrors400.FilePartInvalid.ThrowRpcError();
        }
        var ok = await storage.SavePartAsync(obj.FileId, obj.FilePart, obj.FileTotalParts, obj.Bytes, isBig: false);
        if (!ok)
        {
            RpcErrors.RpcErrors400.FilePartInvalid.ThrowRpcError();
        }
        return new TBoolTrue();
    }
}
