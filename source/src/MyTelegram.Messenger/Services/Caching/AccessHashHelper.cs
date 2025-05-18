namespace MyTelegram.Messenger.Services.Caching;

internal sealed class AccessHashHelper(
    IQueryProcessor queryProcessor,
    IReadModelCacheHelper<IUserReadModel> useReadModelCacheHelper,
    IPeerHelper peerHelper)
    : IAccessHashHelper, ISingletonDependency
{
    private readonly ConcurrentDictionary<long, long> _accessHashCaches = new();

    public void AddAccessHash(long id, long accessHash)
    {
        _accessHashCaches.TryAdd(id, accessHash);
    }


    public async Task<bool> IsAccessHashValidAsync(long id,
        long accessHash, AccessHashType? accessHashType = null)
    {
        if (_accessHashCaches.TryGetValue(id, out var cachedAccessHash))
        {
            return accessHash == cachedAccessHash;
        }
        if (accessHashType == null)
        {
            var peer = peerHelper.GetPeer(id);
            switch (peer.PeerType)
            {
                case PeerType.Channel:
                    accessHashType = AccessHashType.Channel;
                    break;
                case PeerType.User:
                    accessHashType = AccessHashType.User;
                    break;
                case PeerType.Self:
                    return true;
            }
        }

        var accessHashReadModel = await queryProcessor.ProcessAsync(new GetAccessHashQueryByIdQuery(id));

        if (accessHashReadModel != null)
        {
            _accessHashCaches.TryAdd(accessHashReadModel.AccessId, accessHashReadModel.AccessHash);
            return accessHash == accessHashReadModel.AccessHash;
        }

        switch (accessHashType)
        {
            case AccessHashType.User:
                var userReadModel = await useReadModelCacheHelper.GetOrCreateAsync(id,
                    () => queryProcessor.ProcessAsync(new GetUserByIdQuery(id)), p => p.Id);
                if (userReadModel != null)
                {
                    _accessHashCaches.TryAdd(id, userReadModel.AccessHash);
                    return accessHash == userReadModel.AccessHash;
                }

                break;

            case AccessHashType.Channel:
                var channelReadModel = await queryProcessor.ProcessAsync(new GetChannelByIdQuery(id));
                if (channelReadModel != null)
                {
                    _accessHashCaches.TryAdd(id, channelReadModel.AccessHash);
                    return accessHash == channelReadModel.AccessHash;
                }

                break;

            case AccessHashType.WallPaper:
                var wallPaperReadModel = await queryProcessor.ProcessAsync(new GetWallPaperQuery(id));
                if (wallPaperReadModel != null)
                {
                    _accessHashCaches.TryAdd(id, wallPaperReadModel.AccessHash);
                    return accessHash == wallPaperReadModel.AccessHash;
                }
                break;
            case AccessHashType.Theme:
                var themeReadModel = await queryProcessor.ProcessAsync(new GetThemeByIdQuery(id));
                if (themeReadModel != null)
                {
                    _accessHashCaches.TryAdd(id, themeReadModel.Theme.AccessHash);

                    return accessHash == themeReadModel.Theme.AccessHash;
                }
                break;
            case AccessHashType.GroupCall:
                var groupCallReadModel = await queryProcessor.ProcessAsync(new GetGroupCallByIdQuery(id));
                if (groupCallReadModel != null)
                {
                    _accessHashCaches.TryAdd(groupCallReadModel.GroupCallId, groupCallReadModel.AccessHash);

                    return accessHash == groupCallReadModel.AccessHash;
                }
                break;
            case AccessHashType.StickerSet:
                var stickerSetReadModel = await queryProcessor.ProcessAsync(new GetStickerSetByIdQuery(id));
                if (stickerSetReadModel != null)
                {
                    _accessHashCaches.TryAdd(stickerSetReadModel.StickerSetId, stickerSetReadModel.AccessHash);

                    return accessHash == stickerSetReadModel.AccessHash;
                }
                break;
            case AccessHashType.Document:
                var documentReadModel = await queryProcessor.ProcessAsync(new GetDocumentByIdQuery(id));
                if (documentReadModel != null)
                {
                    _accessHashCaches.TryAdd(documentReadModel.DocumentId, documentReadModel.AccessHash);
                    return accessHash == documentReadModel.AccessHash;
                }
                break;
            case AccessHashType.Photo:
                var photoReadModel = await queryProcessor.ProcessAsync(new GetPhotoByIdQuery(id));
                if (photoReadModel != null)
                {
                    _accessHashCaches.TryAdd(photoReadModel.PhotoId, photoReadModel.AccessHash);
                    return accessHash == photoReadModel.AccessHash;
                }
                break;
            //case AccessHashType.Sticker:
            //    break;
            default:
                throw new ArgumentOutOfRangeException(nameof(accessHashType), accessHashType, null);
        }

        return false;
    }

    public async Task CheckAccessHashAsync(long id,
        long accessHash, AccessHashType? accessHashType = null)
    {
        if (!await IsAccessHashValidAsync(id, accessHash, accessHashType))
        {
            switch (accessHashType)
            {
                case AccessHashType.WallPaper:
                    RpcErrors.RpcErrors400.WallpaperInvalid.ThrowRpcError();
                    break;
                case AccessHashType.Theme:
                    RpcErrors.RpcErrors400.ThemeInvalid.ThrowRpcError();
                    break;
                case AccessHashType.GroupCall:
                    RpcErrors.RpcErrors400.GroupcallInvalid.ThrowRpcError();
                    break;
                case AccessHashType.StickerSet:
                    RpcErrors.RpcErrors400.StickersetInvalid.ThrowRpcError();
                    break;
                case AccessHashType.User:
                    RpcErrors.RpcErrors400.UserIdInvalid.ThrowRpcError();
                    break;
                case AccessHashType.Channel:
                    RpcErrors.RpcErrors400.ChannelIdInvalid.ThrowRpcError();
                    break;
                case AccessHashType.Document:
                    RpcErrors.RpcErrors400.DocumentInvalid.ThrowRpcError();
                    break;
                case AccessHashType.Photo:
                    RpcErrors.RpcErrors400.PhotoInvalid.ThrowRpcError();
                    break;
                case AccessHashType.Sticker:
                    RpcErrors.RpcErrors400.StickersetInvalid.ThrowRpcError();
                    break;
                default:
                    RpcErrors.RpcErrors400.PeerIdInvalid.ThrowRpcError();
                    break;
            }
        }
    }

    public Task CheckAccessHashAsync(IInputPeer? inputPeer) =>
        inputPeer switch
        {
            TInputPeerChannel inputPeerChannel => CheckAccessHashAsync(inputPeerChannel.ChannelId,
                inputPeerChannel.AccessHash),
            TInputPeerChannelFromMessage inputPeerChannelFromMessage => CheckAccessHashAsync(inputPeerChannelFromMessage
                .Peer),
            TInputPeerUser inputPeerUser => CheckAccessHashAsync(inputPeerUser.UserId, inputPeerUser.AccessHash),
            TInputPeerUserFromMessage inputPeerUserFromMessage => CheckAccessHashAsync(inputPeerUserFromMessage.Peer),
            _ => Task.CompletedTask
        };

    public Task CheckAccessHashAsync(IInputUser inputUser)
    {
        if (inputUser is TInputUser tInputUser)
        {
            return CheckAccessHashAsync(tInputUser.UserId, tInputUser.AccessHash);
        }

        return Task.CompletedTask;
    }

    public Task CheckAccessHashAsync(IInputChannel inputChannel)
    {
        if (inputChannel is TInputChannel tInputChannel)
        {
            return CheckAccessHashAsync(tInputChannel.ChannelId, tInputChannel.AccessHash, AccessHashType.Channel);
        }

        return Task.CompletedTask;
    }
}

