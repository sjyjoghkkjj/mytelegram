// ReSharper disable All

using MyTelegram.Messenger.Converters.ConverterServices;
using MyTelegram.Schema;
using GetStickerSetByIdQuery = MyTelegram.Queries.GetStickerSetByIdQuery;
using GetWallPaperQuery = MyTelegram.Queries.GetWallPaperQuery;
using TChatFull = MyTelegram.Schema.Messages.TChatFull;
using TStickerSet = MyTelegram.Schema.TStickerSet;

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Get full info about a <a href="https://corefork.telegram.org/api/channel#supergroups">supergroup</a>, <a href="https://corefork.telegram.org/api/channel#gigagroups">gigagroup</a> or <a href="https://corefork.telegram.org/api/channel#channels">channel</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 406 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 403 CHANNEL_PUBLIC_GROUP_NA channel/supergroup not available.
/// 400 CHAT_NOT_MODIFIED No changes were made to chat information because the new information you passed is identical to the current information.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// See <a href="https://corefork.telegram.org/method/channels.getFullChannel" />
///</summary>
internal sealed class GetFullChannelHandler(
    IQueryProcessor queryProcessor,
    //ILayeredService<IChatConverter> layeredService,
    IChatConverterService chatConverterService,
    IAccessHashHelper accessHashHelper,
    IPhotoAppService photoAppService,
    ILogger<GetFullChannelHandler> logger,
    IOptions<MyTelegramMessengerServerOptions> options,
    IChannelAppService channelAppService,
    IChatInviteLinkHelper chatInviteLinkHelper)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetFullChannel, MyTelegram.Schema.Messages.IChatFull>,
        IGetFullChannelHandler
{
    protected override async Task<MyTelegram.Schema.Messages.IChatFull> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetFullChannel obj)
    {
        if (obj.Channel is TInputChannel inputChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(inputChannel.ChannelId, inputChannel.AccessHash);

            var channelReadModel = await channelAppService.GetAsync(inputChannel.ChannelId);
            if (channelReadModel == null)
            {
                RpcErrors.RpcErrors400.ChannelInvalid.ThrowRpcError();
            }

            var channelFullReadModel = await channelAppService.GetChannelFullAsync(inputChannel.ChannelId);
            if (channelFullReadModel == null)
            {
                RpcErrors.RpcErrors400.ChannelInvalid.ThrowRpcError();
            }

            var dialogReadModel = await queryProcessor.ProcessAsync(
                new GetDialogByIdQuery(DialogId.Create(input.UserId, PeerType.Channel, inputChannel.ChannelId)));
            if (dialogReadModel == null)
            {
                logger.LogWarning("Dialog not exists, userId: {UserId}, toPeer: {ToPeer}", input.UserId, new Peer(PeerType.Channel, inputChannel.ChannelId));
            }
            else
            {
                channelFullReadModel!.ReadInboxMaxId = dialogReadModel.ReadInboxMaxId;
                channelFullReadModel.ReadOutboxMaxId = dialogReadModel.ReadOutboxMaxId;
                var maxId = new[]{dialogReadModel.ReadInboxMaxId, dialogReadModel.ReadOutboxMaxId,
                    dialogReadModel.ChannelHistoryMinId}.Max();
                channelFullReadModel.UnreadCount = channelReadModel!.TopMessageId - maxId;
            }

            var channelMember = await queryProcessor
                .ProcessAsync(new GetChannelMemberByUserIdQuery(inputChannel.ChannelId, input.UserId));

            var peerNotifySettings = await queryProcessor
                .ProcessAsync(
                    new GetPeerNotifySettingsByIdQuery(PeerNotifySettingsId.Create(input.UserId,
                        PeerType.Channel,
                        inputChannel.ChannelId)));
            var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);
            IChatInviteReadModel? chatInviteReadModel = null;
            if (channelReadModel.AdminList.Any(p => p.UserId == input.UserId))
            {
                chatInviteReadModel =
                    await queryProcessor.ProcessAsync(new GetPermanentChatInviteQuery(inputChannel.ChannelId));
                if (chatInviteReadModel != null)
                {
                    chatInviteReadModel.Link =
                        chatInviteLinkHelper.GetFullLink(options.Value.JoinChatDomain, chatInviteReadModel.Link);
                }
            }

            var channel = chatConverterService.ToChannel(input.UserId, channelReadModel, photoReadModel, channelMember,
                false, input.Layer);

            var chatFull = chatConverterService.ToChannelFull(
                input.UserId,
                channelReadModel!,
                photoReadModel,
                channelFullReadModel!,
                null,
                peerNotifySettings,
                chatInviteReadModel,
                input.Layer
                );

            var fullChat = chatFull.FullChat;
            IChat? linkedChannel = null;

            if (channelFullReadModel!.LinkedChatId.HasValue)
            {
                var linkedChannelReadModel =
                    await channelAppService.GetAsync(channelFullReadModel.LinkedChatId.Value);
                if (linkedChannelReadModel != null)
                {
                    var linkedChannelPhotoReadModel = await photoAppService.GetAsync(linkedChannelReadModel.PhotoId);
                    var linkedChannelMemberReadModel =
                     await queryProcessor.ProcessAsync(
                            new GetChannelMemberByUserIdQuery(linkedChannelReadModel.ChannelId, input.UserId));
                    linkedChannel = chatConverterService.ToChannel(input.UserId,
                      linkedChannelReadModel, linkedChannelPhotoReadModel, linkedChannelMemberReadModel,
                      linkedChannelMemberReadModel == null || linkedChannelMemberReadModel.Left, input.Layer);

                    //r.Chats.Add(linkedChannel);
                }
            }

            if (linkedChannel != null)
            {
                chatFull.Chats.Add(linkedChannel);
            }

            return chatFull;
        }

        throw new NotImplementedException();
    }
}
