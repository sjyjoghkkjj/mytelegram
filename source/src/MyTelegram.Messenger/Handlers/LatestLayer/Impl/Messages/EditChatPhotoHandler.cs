// ReSharper disable All

namespace MyTelegram.Handlers.Messages;

///<summary>
/// Changes chat photo and sends a service message on it
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// 400 CHAT_NOT_MODIFIED No changes were made to chat information because the new information you passed is identical to the current information.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 400 PHOTO_CROP_SIZE_SMALL Photo is too small.
/// 400 PHOTO_EXT_INVALID The extension of the photo is invalid.
/// 400 PHOTO_INVALID Photo invalid.
/// See <a href="https://corefork.telegram.org/method/messages.editChatPhoto" />
///</summary>
internal sealed class EditChatPhotoHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestEditChatPhoto, MyTelegram.Schema.IUpdates>,
    Messages.IEditChatPhotoHandler
{
    private readonly ICommandBus _commandBus;
    private readonly IMediaHelper _mediaHelper;
    private readonly IRandomHelper _randomHelper;

    public EditChatPhotoHandler(IMediaHelper mediaHelper,
        ICommandBus commandBus,
        IRandomHelper randomHelper)
    {
        _mediaHelper = mediaHelper;
        _commandBus = commandBus;
        _randomHelper = randomHelper;
    }

    protected override async Task<IUpdates> HandleCoreAsync(IRequestInput input,
        RequestEditChatPhoto obj)
    {
        //var photo=await _mediaHelper.SavePhotoAsync(input.ReqMsgId,obj)
        var chatId = obj.ChatId;
        long fileId = 0;
        var parts = 0;
        var md5 = string.Empty;
        var name = string.Empty;
        var hasVideo = false;
        double? videoStartTs = 0;
        IVideoSize? videoSize = null;
        switch (obj.Photo)
        {
            case Schema.TInputChatUploadedPhoto inputChatUploadedPhoto:
                {
                    var file = inputChatUploadedPhoto.File ?? inputChatUploadedPhoto.Video;
                    if (file != null && file is TInputFile tInputFile)
                    {
                        //ThrowHelper.ThrowUserFriendlyException("PHOTO_INVALID");
                        //RpcErrors.RpcErrors400.PhotoInvalid.ThrowRpcError();

                        fileId = tInputFile!.Id;
                        parts = tInputFile.Parts;
                        name = tInputFile.Name;
                        hasVideo = inputChatUploadedPhoto.Video != null;
                        videoStartTs = inputChatUploadedPhoto.VideoStartTs;
                        switch (file)
                        {
                            case TInputFile inputFile:
                                md5 = inputFile.Md5Checksum;
                                break;
                            case TInputFileBig:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(file));
                        }
                    }

                    videoSize = inputChatUploadedPhoto.VideoEmojiMarkup;
                }
                break;
            case TInputChatPhoto inputChatPhoto:
                //photo=await _mediaHelper.SavePhotoAsync(input.ReqMsgId,inputChatPhoto.)
                switch (inputChatPhoto.Id)
                {
                    case TInputPhoto inputPhoto:
                        fileId = inputPhoto.Id;
                        break;
                    case TInputPhotoEmpty:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case TInputChatPhotoEmpty:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var r = await _mediaHelper.SavePhotoAsync(input.ReqMsgId,
            fileId,
            input.UserId,
            hasVideo,
            videoStartTs,
            parts,
            name,
            md5,
            videoSize
            );
        var command = new EditChatPhotoCommand(ChatId.Create(chatId),
            input.ToRequestInfo(),
            fileId,
            r.PhotoId,
            //photo.ToBytes(),
            new TMessageActionChatEditPhoto { Photo = r.Photo }.ToBytes().ToHexString(),
            _randomHelper.NextInt64());
        await _commandBus.PublishAsync(command, CancellationToken.None);

        return null!;
    }
}
