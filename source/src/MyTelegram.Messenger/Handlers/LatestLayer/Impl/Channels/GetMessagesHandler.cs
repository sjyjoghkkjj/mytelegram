// ReSharper disable All

namespace MyTelegram.Handlers.Channels;

///<summary>
/// Get <a href="https://corefork.telegram.org/api/channel">channel/supergroup</a> messages
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 406 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 MESSAGE_IDS_EMPTY No message ids were provided.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 USER_BANNED_IN_CHANNEL You're banned from sending messages in supergroups/channels.
/// See <a href="https://corefork.telegram.org/method/channels.getMessages" />
///</summary>
internal sealed class GetMessagesHandler(
    IMessageAppService messageAppService,
    IAccessHashHelper accessHashHelper,
    IChannelAppService channelAppService,
    ILayeredService<IRpcResultProcessor> layeredService)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetMessages, MyTelegram.Schema.Messages.IMessages>,
        Channels.IGetMessagesHandler
{
    protected override async Task<IMessages> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetMessages obj)
    {
        long channelId = 0;
        if (obj.Channel is TInputChannel inputChannel)
        {
            channelId = inputChannel.ChannelId;
            var channelReadModel = await channelAppService.GetAsync(inputChannel.ChannelId);
            if (channelReadModel == null)
            {
                RpcErrors.RpcErrors400.ChannelIdInvalid.ThrowRpcError();
            }

            // Only check accessHash for private channel
            if (string.IsNullOrEmpty(channelReadModel!.UserName))
            {
                await accessHashHelper.CheckAccessHashAsync(inputChannel.ChannelId, inputChannel.AccessHash);
            }
        }
        else
        {
            RpcErrors.RpcErrors400.ChannelIdInvalid.ThrowRpcError();
        }
        var idList = new List<int>();
        foreach (var inputMessage in obj.Id)
        {
            if (inputMessage is TInputMessageID inputMessageId)
            {
                idList.Add(inputMessageId.Id);
            }
        }

        var dto = await messageAppService
            .GetMessagesAsync(
                new GetMessagesInput(input.UserId,
                    channelId,
                    idList,
                    new Peer(PeerType.Channel, channelId))
                { Limit = 50 });

        return layeredService.GetConverter(input.Layer).ToMessages(dto, input.Layer);
    }
}