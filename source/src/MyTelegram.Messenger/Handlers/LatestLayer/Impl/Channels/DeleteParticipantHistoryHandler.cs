// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Delete all messages sent by a specific participant of a given supergroup
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 PARTICIPANT_ID_INVALID The specified participant ID is invalid.
/// See <a href="https://corefork.telegram.org/method/channels.deleteParticipantHistory" />
///</summary>
internal sealed class DeleteParticipantHistoryHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestDeleteParticipantHistory, MyTelegram.Schema.Messages.IAffectedHistory>,
    Channels.IDeleteParticipantHistoryHandler
{
    private readonly IQueryProcessor _queryProcessor;
    private readonly ICommandBus _commandBus;
    private readonly IPeerHelper _peerHelper;
    private readonly IPtsHelper _ptsHelper;
    private readonly IAccessHashHelper _accessHashHelper;
    private readonly IChannelAdminRightsChecker _channelAdminRightsChecker;
    public DeleteParticipantHistoryHandler(IQueryProcessor queryProcessor,
        ICommandBus commandBus,
        IPeerHelper peerHelper,
        IPtsHelper ptsHelper,
        IAccessHashHelper accessHashHelper, IChannelAdminRightsChecker channelAdminRightsChecker)
    {
        _queryProcessor = queryProcessor;
        _commandBus = commandBus;
        _peerHelper = peerHelper;
        _ptsHelper = ptsHelper;
        _accessHashHelper = accessHashHelper;
        _channelAdminRightsChecker = channelAdminRightsChecker;
    }

    protected override async Task<MyTelegram.Schema.Messages.IAffectedHistory> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestDeleteParticipantHistory obj)
    {
        if (obj.Channel is TInputChannel inputChannel)
        {
            await _accessHashHelper.CheckAccessHashAsync(inputChannel.ChannelId, inputChannel.AccessHash);
            await _channelAdminRightsChecker.CheckAdminRightAsync(inputChannel.ChannelId, input.UserId,
                rights => rights.AdminRights.DeleteMessages, RpcErrors.RpcErrors403.ChatAdminRequired);

            var peer = _peerHelper.GetPeer(obj.Participant);
            var messageIds = (await _queryProcessor
                .ProcessAsync(new GetMessageIdListByUserIdQuery(inputChannel.ChannelId,
                    peer.PeerId,
                    MyTelegramServerDomainConsts.ClearHistoryDefaultPageSize))).ToList();

            if (messageIds.Count > 0)
            {
                var newTopMessageId =
                    await _queryProcessor.ProcessAsync(new GetTopMessageIdQuery(inputChannel.ChannelId,
                        messageIds));

                var command = new StartDeleteParticipantHistoryCommand(TempId.New,
                    input.ToRequestInfo(),
                    inputChannel.ChannelId,
                    messageIds,
                    newTopMessageId
                );
                await _commandBus.PublishAsync(command);

                return null!;
            }
            else
            {
                return new TAffectedHistory
                {
                    Pts = _ptsHelper.GetCachedPts(inputChannel.ChannelId),
                    PtsCount = 0,
                    Offset = 0
                };
            }
        }

        throw new NotImplementedException();
    }
}
