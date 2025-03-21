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
    IUserConverterService userConverterService,
    ILayeredService<ISendAsPeerConverter> layeredSendAsPeerService,
    IAccessHashHelper accessHashHelper,
    IPhotoAppService photoAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetSendAs, MyTelegram.Schema.Channels.ISendAsPeers>,
        IGetSendAsHandler
{
    protected override async Task<MyTelegram.Schema.Channels.ISendAsPeers> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetSendAs obj)
    {
        if (obj.Peer is TInputPeerEmpty)
        {
            return new TSendAsPeers
            {
                Chats = [],
                Peers = [],
                Users = []
            };
        }

        if (obj.Peer is TInputPeerChannel inputPeerChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(inputPeerChannel.ChannelId, inputPeerChannel.AccessHash);
            var channelReadModels = await queryProcessor.ProcessAsync(new GetSendAsQuery(inputPeerChannel.ChannelId));
            var channelReadModel = channelReadModels.FirstOrDefault(p => p.ChannelId == inputPeerChannel.ChannelId);
            if (channelReadModel == null)
            {
                RpcErrors.RpcErrors400.ChannelIdInvalid.ThrowRpcError();
            }

            if (channelReadModels.Any(p => p.CreatorId != input.UserId))
            {
                var user = await userConverterService.GetUserAsync(input.UserId, input.UserId, layer: input.Layer);

                return new TSendAsPeers
                {
                    Chats = [],
                    Peers = new TVector<ISendAsPeer>([new TSendAsPeer
                    {
                        Peer=new TPeerUser
                        {
                            UserId=input.UserId,
                        }
                    }]),
                    Users = new TVector<IUser>([user])
                };
            }

            var photoReadModels = await photoAppService.GetPhotosAsync(channelReadModels);
            var channels = chatConverterService.ToChannelList(input.UserId, channelReadModels,
                photoReadModels, [], [], false, input.Layer);

            var r = layeredSendAsPeerService.GetConverter(input.Layer).ToSendAsPeers(channels);
            if (channelReadModel!.Signatures && channelReadModel.CreatorId == input.UserId)
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

        throw new NotImplementedException();
    }
}
