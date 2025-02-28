namespace MyTelegram.GatewayServer.Services;

public class ClientDataSender(
    IClientManager clientManager,
    ILogger<ClientDataSender> logger,
    IMessageQueueProcessor<ClientDisconnectedEvent> messageQueueProcessor,
    IMtpMessageEncoder messageEncoder)
    : IClientDataSender, ITransientDependency
{
    public Task SendAsync(MTProto.UnencryptedMessageResponse data)
    {
        if (!clientManager.TryGetClientData(data.ConnectionId, out var d))
        {
            logger.LogWarning("[0] Cannot find cached client info, skip sending message, connectionId: {ConnectionId}", data.ConnectionId);
            return Task.CompletedTask;
        }

        var encodedBytes = ArrayPool<byte>.Shared.Rent(GetEncodedDataMaxLength(data.Data.Length));
        try
        {
            var totalCount = messageEncoder.Encode(d, data, encodedBytes);

            return SendAsync(encodedBytes.AsMemory()[..totalCount], d);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(encodedBytes, true);
        }
    }

    public Task SendAsync(MTProto.EncryptedMessageResponse data)
    {
        if (!clientManager.TryGetClientData(data.ConnectionId, out var d))
        {
            if (!clientManager.TryGetClientData(data.AuthKeyId, out d))
            {
                logger.LogWarning(
                    "Cannot find cached client info, skip sending message, connectionId: {ConnectionId}, authKeyId: {AuthKeyId}",
                    data.ConnectionId,
                    data.AuthKeyId
                );
                messageQueueProcessor.Enqueue(new ClientDisconnectedEvent(data.ConnectionId, data.AuthKeyId, 0), 0);
                return Task.CompletedTask;
            }
        }

        d.ResponseQueue.Writer.TryWrite(data);

        return Task.CompletedTask;
    }

    public int EncodeData(MTProto.EncryptedMessageResponse data, ClientData d, byte[] encodedBytes)
    {
        return messageEncoder.Encode(d, data, encodedBytes);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> encodedBytes,
        ClientData clientData)
    {
        switch (clientData.ClientType)
        {
            case ClientType.Tcp:
                await clientData.ConnectionContext!.Transport.Output.WriteAsync(encodedBytes);
                await clientData.ConnectionContext!.Transport.Output.FlushAsync();

                break;

            case ClientType.WebSocket:
                await clientData.WebSocket!.SendAsync(encodedBytes, WebSocketMessageType.Binary, true, default);

                break;
        }
    }

    public int GetEncodedDataMaxLength(int messageDataLength)
    {
        // LengthBytes=1~19 Abridged:1/4 | Intermediate:4 | Padded intermediate:4+(0~15) | Full:12
        // length(use max length 20),authKeyId(8),messageId(8),messageDataLength(4),messageData
        return 20 + 8 + 8 + 4 + messageDataLength;
    }
}
