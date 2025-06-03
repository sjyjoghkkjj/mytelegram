namespace MyTelegram.Services.Services;

public class QueuedObjectMessageSender(
    IMessageQueueProcessor<ISessionMessage> sessionMessageQueueProcessor,
    IGZipHelper gzipHelper)
    : IObjectMessageSender, ITransientDependency
{
    public Task PushSessionMessageToAuthKeyIdAsync<TData>(long authKeyId,
        TData data,
        int pts = 0,
        int? qts = null,
        long globalSeqNo = 0, LayeredData<TData>? layeredData = null) where TData : IObject
    {
        var layeredByteData = layeredData?.DataWithLayer?.ToDictionary(k => k.Key, v => v.Value.ToBytes());

        sessionMessageQueueProcessor.Enqueue(new LayeredAuthKeyIdMessageCreatedIntegrationEvent(authKeyId,
                data.ToBytes(),
                pts,
                qts,
                globalSeqNo, new LayeredData<byte[]>(layeredByteData)),
            authKeyId);

        return Task.CompletedTask;
    }

    public Task PushMessageToPeerAsync<TData>(Peer peer,
        TData data,
        long? excludeAuthKeyId = null,
        long? excludeUserId = null,
        long? onlySendToUserId = null,
        long? onlySendToThisAuthKeyId = null,
        int pts = 0,
        int? qts = null,
        long globalSeqNo = 0,
        LayeredData<TData>? layeredData = null,
        PushData? pushData = null,
        List<long>? excludeUserIds = null
        ) where TData : IObject
    {
        sessionMessageQueueProcessor.Enqueue(new LayeredPushMessageCreatedIntegrationEvent((int)peer.PeerType,
                peer.PeerId,
                data.ToBytes(),
                excludeAuthKeyId,
                excludeUserId,
                onlySendToUserId,
                onlySendToThisAuthKeyId,
                pts,
                qts,
                globalSeqNo,
                new LayeredData<byte[]>(layeredData?.DataWithLayer?.ToDictionary(k => k.Key, v => v.Value.ToBytes())),
                PushData: pushData,
                excludeUserIds
            ),
            peer.PeerId);

        return Task.CompletedTask;
    }

    public Task PushMessageToPeerAsync<TData, TExtraData>(Peer peer,
        TData data,
        long? excludeAuthKeyId = null,
        long? excludeUserId = null,
        long? onlySendToUserId = null,
        long? onlySendToThisAuthKeyId = null,
        int pts = 0,
        int? qts = null,
        long globalSeqNo = 0,
        LayeredData<TData>? layeredData = null,
        TExtraData? extraData = default,
        PushData? pushData = null,
        List<long>? excludeUserIds = null
        ) where TData : IObject
    {
        if (extraData == null)
        {
            return PushMessageToPeerAsync(peer,
                data,
                excludeAuthKeyId,
                excludeUserId,
                onlySendToUserId,
                onlySendToThisAuthKeyId,
                pts,
                qts,
                globalSeqNo,
                layeredData,
                pushData,
                excludeUserIds
                );
        }

        sessionMessageQueueProcessor.Enqueue(new LayeredPushMessageCreatedIntegrationEvent<TExtraData>(
                (int)peer.PeerType,
                peer.PeerId,
                data.ToBytes(),
                excludeAuthKeyId,
                excludeUserId,
                onlySendToUserId,
                onlySendToThisAuthKeyId,
                pts,
                qts,
                globalSeqNo,
                new LayeredData<byte[]>(layeredData?.DataWithLayer?.ToDictionary(k => k.Key, v => v.Value.ToBytes())),
                extraData,
                pushData,
                excludeUserIds
            ),
            peer.PeerId);

        return Task.CompletedTask;
    }

    public Task SendMessageToPeerAsync<TData>(RequestInfo requestInfo,
        TData data) where TData : IObject
    {
        sessionMessageQueueProcessor.Enqueue(new DataResultResponseReceivedEvent(requestInfo.ReqMsgId, data.ToBytes()),
            requestInfo.PermAuthKeyId);

        return Task.CompletedTask;
    }

    public Task SendFileDataToPeerAsync<TData>(RequestInfo requestInfo,
        TData data) where TData : IObject
    {
        sessionMessageQueueProcessor.Enqueue(new FileDataResultResponseReceivedEvent(requestInfo.ReqMsgId, data.ToBytes()),
            requestInfo.PermAuthKeyId);

        return Task.CompletedTask;
    }

    public Task SendRpcMessageToClientAsync<TData>(RequestInfo requestInfo,
        TData data,
        int pts = 0) where TData : IObject
    {
        return SendRpcMessageToClientAsync(requestInfo.ReqMsgId, data, pts, requestInfo.PermAuthKeyId);
    }

    public Task SendRpcMessageToClientAsync<TData>(long reqMsgId, TData data, int pts = 0, long permAuthKeyId = 0) where TData : IObject
    {
        var rpcResult = CreateRpcResult(reqMsgId, data);

        sessionMessageQueueProcessor.Enqueue(new DataResultResponseReceivedEvent(reqMsgId, rpcResult.ToBytes()),
            permAuthKeyId);

        return Task.CompletedTask;
    }

    public Task SendRpcMessageToClientAsync<TData>(RequestInfo requestInfo, TData data,
        long authKeyId, long permAuthKeyId, long userId,
        int pts = 0) where TData : IObject
    {
        var rpcResult = CreateRpcResult(requestInfo.ReqMsgId, data);
        sessionMessageQueueProcessor.Enqueue(
            new DataResultResponseWithUserIdReceivedEvent(requestInfo.ReqMsgId, rpcResult.ToBytes(), userId, authKeyId,
                permAuthKeyId),
            requestInfo.PermAuthKeyId);

        return Task.CompletedTask;
    }

    private TRpcResult CreateRpcResult<TData>(long reqMsgId, TData data) where TData : IObject
    {
        var newData = data;
        var rpcResult = new TRpcResult { ReqMsgId = reqMsgId, Result = data };

        var length = data.GetLength();
        if (length > 500)
        {
            var gzipPacked = new TGzipPacked
            {
                PackedData = gzipHelper.Compress(newData.ToBytes())
            };
            rpcResult.Result = gzipPacked;
        }

        return rpcResult;
    }
}