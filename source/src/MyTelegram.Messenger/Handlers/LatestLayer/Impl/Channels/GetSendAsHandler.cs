namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Obtains a list of peers that can be used to send messages in a specific group
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/channels.getSendAs" />
///</summary>
internal sealed class GetSendAsHandler(
    IQueryProcessor queryProcessor,
    IChatConverterService chatConverterService,
    IChannelAppService channelAppService,
    ILayeredService<ISendAsPeerConverter> layeredSendAsPeerService,
    IAccessHashHelper accessHashHelper,
    IPhotoAppService photoAppService)
    : RpcResultObjectHandler<RequestGetSendAs, ISendAsPeers>,
        IGetSendAsHandler
{
    protected override async Task<ISendAsPeers> HandleCoreAsync(IRequestInput input,
        RequestGetSendAs obj)
    {
        if (obj.Peer is TInputPeerChannel inputPeerChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(inputPeerChannel.ChannelId, inputPeerChannel.AccessHash);

            var channelReadModel = await channelAppService.GetAsync(inputPeerChannel.ChannelId);
            // 1. Super group with linked channel
            // 2. Channel: signature: true 
            // 3. Linked private channel
            if (channelReadModel is { MegaGroup: true, LinkedChatId: not null } or { Broadcast: true, Signatures: true })
            {
                var channelReadModels = await queryProcessor.ProcessAsync(new GetSendAsQuery(input.UserId));

                if (channelReadModel.MegaGroup && channelReadModel.CreatorId == input.UserId)
                {
                    channelReadModels = [.. channelReadModels, channelReadModel];

                    var linkedChannelReadModel = await channelAppService.GetAsync(channelReadModel.LinkedChatId);
                    if (linkedChannelReadModel != null && linkedChannelReadModel.CreatorId == input.UserId)
                    {
                        channelReadModels = [.. channelReadModels, linkedChannelReadModel];
                    }
                }

                var channelMemberReadModels = await queryProcessor.ProcessAsync(
                    new GetChannelMemberListByChannelIdListQuery(input.UserId,
                        [.. channelReadModels.Select(p => p.ChannelId)]));
                var photoReadModels = await photoAppService.GetPhotosAsync(channelReadModels);
                var channels = chatConverterService.ToChannelList(input.UserId, channelReadModels,
                    photoReadModels, channelMemberReadModels, layer: input.Layer);

                var r = layeredSendAsPeerService.GetConverter(input.Layer).ToSendAsPeers(channels);
                if (channelReadModel.Signatures && channelReadModel.CreatorId == input.UserId)
                {
                    r.Peers.Add(new TSendAsPeer
                    {
                        Peer = new TPeerUser
                        {
                            UserId = input.UserId
                        }
                    });
                }

                return r;
            }
        }

        return new TSendAsPeers
        {
            Chats = [],
            Peers = [],
            Users = []
        };
    }
}
