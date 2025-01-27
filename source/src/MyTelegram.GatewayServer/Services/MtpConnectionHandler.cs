namespace MyTelegram.GatewayServer.Services;

public class MtpConnectionHandler(
    IClientManager clientManager,
    IMtpMessageParser messageParser,
    IMtpMessageDispatcher messageDispatcher,
    ILogger<MtpConnectionHandler> logger,
    IClientDataSender clientDataSender,
    IMessageQueueProcessor<ClientDisconnectedEvent> messageQueueProcessor)
    : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var remoteEndPoint = connection.RemoteEndPoint;
        var proxyProtocolFeature = connection.Features.Get<ProxyProtocolFeature>();
        var clientIp = (connection.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? string.Empty;
        if (proxyProtocolFeature != null)
        {
            remoteEndPoint = new IPEndPoint(proxyProtocolFeature.SourceIp, proxyProtocolFeature.SourcePort);
            clientIp = proxyProtocolFeature.SourceIp.ToString();
        }

        var connectionTypeFeature = connection.Features.Get<ConnectionTypeFeature>();

        logger.LogInformation(
            "[ConnectionId: {ConnectionId}] New client connected, localPort: {LocalPort}({ConnectionType}), remoteEndPoint: {RemoteEndPoint}, online count: {OnlineCount}",
            connection.ConnectionId,
            (connection.LocalEndPoint as IPEndPoint)?.Port,
            connectionTypeFeature?.ConnectionType,
            remoteEndPoint,
            clientManager.GetOnlineCount());

        var clientData = new ClientData
        {
            ConnectionContext = connection,
            ConnectionId = connection.ConnectionId,
            ClientType = ClientType.Tcp,
            ClientIp = clientIp,
            ConnectionType = connectionTypeFeature?.ConnectionType ?? ConnectionType.Generic
        };
        clientManager.AddClient(connection.ConnectionId, clientData);

        connection.ConnectionClosed.Register(() =>
        {
            if (clientManager.TryRemoveClient(connection.ConnectionId, out _))
            {
                messageQueueProcessor.Enqueue(
                    new ClientDisconnectedEvent(clientData.ConnectionId, clientData.AuthKeyId, 0),
                    clientData.AuthKeyId);
            }

            logger.LogInformation(
                "[ConnectionId: {ConnectionId}] Client disconnected, RemoteEndPoint: {RemoteEndPoint}",
                connection.ConnectionId,
                remoteEndPoint);
        });

        var processSendDataTask = ProcessSendDataAsync(clientData, connection);
        var processReceiveDataTask = ProcessReceiveDataAsync(clientData, connection);

        await Task.WhenAny(processSendDataTask, processReceiveDataTask);
    }

    private async Task ProcessReceiveDataAsync(ClientData clientData, ConnectionContext connection)
    {
        var input = connection.Transport.Input;
        while (!connection.ConnectionClosed.IsCancellationRequested)
        {
            var result = await input.ReadAsync();
            if (result.IsCanceled)
            {
                break;
            }

            var buffer = result.Buffer;
            if (buffer.Length == 0)
            {
                continue;
            }

            if (!clientManager.TryGetClientData(connection.ConnectionId, out _))
            {
                logger.LogWarning("Cannot find client data, connectionId: {ConnectionId}", connection.ConnectionId);
                break;
            }

            if (!clientData.IsFirstPacketParsed)
            {
                //if (buffer.Length < 4)
                //{
                //    continue;
                //}

                messageParser.ProcessFirstUnencryptedPacket(ref buffer, clientData);
            }

            while (TryParseMessage(ref buffer, clientData, out var mtpMessage))
            {
                await ProcessDataAsync(mtpMessage, clientData);
            }

            input.AdvanceTo(buffer.Start, buffer.End);
            if (result.IsCompleted || result.IsCanceled)
            {
                break;
            }
        }

        await input.CompleteAsync();
    }

    private async Task ProcessSendDataAsync(ClientData clientData, ConnectionContext connectionContext)
    {
        var queue = clientData.ResponseQueue;
        while (await queue.Reader.WaitToReadAsync() && !connectionContext.ConnectionClosed.IsCancellationRequested)
        {
            while (queue.Reader.TryRead(out var response))
            {
                var encodedBytes =
                    ArrayPool<byte>.Shared.Rent(clientDataSender.GetEncodedDataMaxLength(response.Data.Length));
                try
                {
                    var totalCount = clientDataSender.EncodeData(response, clientData, encodedBytes);
                    await connectionContext.Transport.Output.WriteAsync(encodedBytes.AsMemory()[..totalCount]);
                    await connectionContext.Transport.Output.FlushAsync();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(encodedBytes);
                }
            }
        }
    }

    private Task ProcessDataAsync(IMtpMessage mtpMessage,
        ClientData clientData)
    {
        if (clientData.IsFirstPacketParsed)
        {
            mtpMessage.ConnectionId = clientData.ConnectionId;
            mtpMessage.ClientIp = clientData.ClientIp;
            mtpMessage.ConnectionType = (int)clientData.ConnectionType;
            //mtpMessage.ClientIp = (clientData.ConnectionContext!.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? string.Empty;
            return messageDispatcher.DispatchAsync(mtpMessage);
        }

        return Task.CompletedTask;
    }

    private bool TryParseMessage(ref ReadOnlySequence<byte> buffer,
        ClientData clientData,
        [NotNullWhen(true)] out IMtpMessage? mtpMessage)
    {
        if (buffer.Length == 0)
        {
            mtpMessage = null;
            return false;
        }

        var reader = new SequenceReader<byte>(buffer);

        if (reader.Remaining < 4)
        {
            mtpMessage = null;

            return false;
        }

        return messageParser.TryParse(ref buffer, clientData, out mtpMessage);
    }
}