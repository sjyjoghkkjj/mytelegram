using Google.Protobuf;
using Grpc.Core;
using MyTelegram.GrpcService;
using MyTelegram.Messenger.Services.Impl;

namespace MyTelegram.MessengerServer.GrpcService.Services;

public class MediaServiceImpl(IFileStorage storage, ILogger<MediaServiceImpl> logger) : MediaService.MediaServiceBase
{
    public override Task<SaveMediaResponse> SaveMedia(SaveMediaRequest request, ServerCallContext context)
    {
        // The media payload already contains TL-serialized IMessageMedia. For demo, just echo back.
        return Task.FromResult(new SaveMediaResponse { Media = request.Media });
    }

    public override Task<SaveEncryptedFileResponse> SaveEncryptedFile(SaveEncryptedFileRequest request, ServerCallContext context)
    {
        // Persist encrypted file blob with a generated id; For demo, use reqMsgId as id.
        var id = request.ReqMsgId != 0 ? request.ReqMsgId : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var bytes = request.EncryptedFile.Span.ToArray();
        // Store as one-part assembled file
        var fileId = id;
        storage.SavePartAsync(fileId, 0, 1, bytes, false).GetAwaiter().GetResult();
        storage.TryAssembleAsync(fileId, 1, false).GetAwaiter().GetResult();
        return Task.FromResult(new SaveEncryptedFileResponse
        {
            Id = fileId,
            DcId = 1,
            AccessHash = 0,
            KeyFingerprint = 0,
            Size = bytes.LongLength
        });
    }

    public override Task<SavePhotoResponse> SavePhoto(SavePhotoRequest request, ServerCallContext context)
    {
        // Persist raw upload (assembled by upload RPCs). For demo, just return back stub photo bytes.
        // In production, decode multipart fileId and build proper TPhoto with sizes.
        var photo = ByteString.CopyFrom(new byte[] { });
        return Task.FromResult(new SavePhotoResponse
        {
            Photo = photo,
            PhotoId = request.FileId,
            Size = 0
        });
    }

    public override Task<CreateDocumentResponse> CreateDocument(CreateDocumentRequest request, ServerCallContext context)
    {
        return Task.FromResult(new CreateDocumentResponse { Success = true });
    }

    public override Task<SaveFileDataResponse> SaveFile(SaveFileDataRequest request, ServerCallContext context)
    {
        var ok = storage.SavePartAsync(request.Id, 0, 1, request.Data.Span, false).GetAwaiter().GetResult();
        if (ok)
        {
            storage.TryAssembleAsync(request.Id, 1, false).GetAwaiter().GetResult();
        }
        return Task.FromResult(new SaveFileDataResponse { Success = ok });
    }

    public override Task<CheckFileExistsResponse> Exists(CheckFileExistsRequest request, ServerCallContext context)
    {
        // Map fileName to id (demo not implemented). Always false.
        return Task.FromResult(new CheckFileExistsResponse { Exists = false });
    }
}

