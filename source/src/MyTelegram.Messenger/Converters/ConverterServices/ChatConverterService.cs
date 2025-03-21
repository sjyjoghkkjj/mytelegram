using IChannelParticipant = MyTelegram.Schema.IChannelParticipant;
using IChatFull = MyTelegram.Schema.IChatFull;
using TChannelParticipant = MyTelegram.Schema.Channels.TChannelParticipant;
using TChatFull = MyTelegram.Schema.Messages.TChatFull;

namespace MyTelegram.Messenger.Converters.ConverterServices;

public class ChatConverterService(
    IQueryProcessor queryProcessor,
    IPhotoAppService photoAppService,
    IChannelAppService channelAppService,
    ILayeredService<IChannelConverter> channelLayeredService,
    ILayeredService<IPhotoConverter> photoLayeredService,
    ILayeredService<IChannelFullConverter> channelFullLayeredService,
    ILayeredService<IChannelParticipantConverter> channelParticipantLayeredService,
    ILayeredService<IChannelParticipantSelfConverter> channelParticipantSelfLayeredService,
    ILayeredService<IPeerNotifySettingsConverter> peerNotifySettingsLayeredService,
    ILayeredService<IChatAdminRightsConverter> chatAdminRightsLayeredService,
    IChatInviteExportedConverterService chatInviteExportedConverterService,
    ILayeredService<IEmojiStatusConverter> emojiStatusLayeredService,
    ILayeredService<IChatBannedRightsConverter> chatBannedRightsLayeredService)
    : IChatConverterService, ITransientDependency
{
    public async Task<IChat> GetChannelAsync(long selfUserId, long channelId,
        bool checkChannelMember, bool? channelMemberIsLeft, int layer = 0)
    {
        var channelReadModel = await channelAppService.GetAsync(channelId);
        if (channelReadModel == null)
        {
            throw new RpcException(RpcErrors.RpcErrors400.ChannelInvalid);
        }

        IChannelMemberReadModel? channelMemberReadModel = null;
        if (checkChannelMember && channelMemberIsLeft == null)
        {
            channelMemberReadModel =
                await queryProcessor.ProcessAsync(new GetChannelMemberByUserIdQuery(channelId, selfUserId));
        }

        var photoReadModel = await photoAppService.GetAsync(channelReadModel.PhotoId);

        return ToChannelCore(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, channelMemberIsLeft,
            layer);
    }

    public async Task<List<IChat>> GetChannelListAsync(long selfUserId,
        List<long> channelIds,
        IReadOnlyCollection<IChannelMemberReadModel>? channelMemberReadModels = null,
        int layer = 0)
    {
        var channels = new List<IChat>();
        var channelReadModels = await channelAppService.GetListAsync(channelIds);
        var photoReadModels = await photoAppService.GetPhotosAsync(channelReadModels);
        var channelMembers = channelMemberReadModels?.ToDictionary(k => k.ChannelId) ?? [];
        var photos = photoReadModels.ToDictionary(k => k.PhotoId);

        foreach (var channelReadModel in channelReadModels)
        {
            photos.TryGetValue(channelReadModel.PhotoId ?? 0, out var photoReadModel);
            channelMembers.TryGetValue(channelReadModel.ChannelId, out var channelMemberReadModel);

            var channel = ToChannelCore(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, false,
                layer);

            channels.Add(channel);
        }

        return channels;
    }

    public async Task<IChatFull> GetChannelFullAsync(long selfUserId, long channelId,
        IPeerNotifySettingsReadModel? peerNotifySettingsReadModel = null,
        IChatInviteReadModel? chatInviteReadModel = null,
        int layer = 0)
    {
        var channelReadModel = await channelAppService.GetAsync(channelId);
        var channelFullReadModel = await channelAppService.GetChannelFullAsync(channelId);
        if (channelReadModel == null || channelFullReadModel == null)
        {
            throw new RpcException(RpcErrors.RpcErrors400.ChannelInvalid);
        }

        var photoReadModel = await photoAppService.GetAsync(channelReadModel.PhotoId);
        return ToChannelFull(selfUserId, channelReadModel, photoReadModel, channelFullReadModel,
            peerNotifySettingsReadModel, chatInviteReadModel, layer);
    }

    public Schema.Channels.IChannelParticipant ToChannelParticipant(
        long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel channelMemberReadModel,
        IUser user,
        int layer = 0
    )
    {
        var participant = ToChannelParticipantCore(selfUserId, channelReadModel, channelMemberReadModel, layer);
        var channel = ToChannel(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, null, layer);
        return new TChannelParticipant
        {
            Chats = new TVector<IChat>(channel),
            Participant = participant,
            Users = new TVector<IUser>(user)
        };
    }

    public IChat ToChannel(long selfUserId, IChannelReadModel channelReadModel, IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel? channelMemberReadModel, bool? channelMemberIsLeft, int layer)
    {
        return ToChannelCore(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, channelMemberIsLeft,
            layer);
    }

    public List<IChat> ToChannelList(long selfUserId, IReadOnlyCollection<IChannelReadModel> channelReadModels,
        IReadOnlyCollection<IPhotoReadModel> photoReadModels,
        IReadOnlyCollection<IChannelMemberReadModel>? channelMemberReadModels,
        IReadOnlyCollection<long>? joinedChannelIds = null, int layer = 0)
    {
        var channels = new List<IChat>();
        var channelMembers = channelMemberReadModels?.ToDictionary(k => k.ChannelId) ?? [];
        var photos = photoReadModels.ToDictionary(k => k.PhotoId);
        var shouldCheckJoinedChannelList = joinedChannelIds != null;
        foreach (var channelReadModel in channelReadModels)
        {
            photos.TryGetValue(channelReadModel.PhotoId ?? 0, out var photoReadModel);
            channelMembers.TryGetValue(channelReadModel.ChannelId, out var channelMemberReadModel);

            var channel = ToChannelCore(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, null,
                layer);
            if (channel is ILayeredChannel chat)
            {
                if (shouldCheckJoinedChannelList)
                {
                    chat.Left = !joinedChannelIds!.Contains(channelReadModel.ChannelId);
                }
            }

            channels.Add(channel);
        }

        return channels;
    }

    public IChannelParticipants ToChannelParticipants(long selfUserId, IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel, IReadOnlyCollection<IChatAdminReadModel>? chatAdminReadModels,
        IReadOnlyCollection<IChannelMemberReadModel> channelMemberReadModels, IEnumerable<IUser> users,
        DeviceType deviceType, bool forceNotLeft, int layer)
    {
        var channelMemberReadModel = channelMemberReadModels.FirstOrDefault(p => p.UserId == selfUserId);
        var channelMemberIsLeft = true;
        if (channelMemberReadModel == null)
        {
            if (forceNotLeft)
            {
                channelMemberIsLeft = false;
            }
        }
        else
        {
            channelMemberIsLeft = channelMemberReadModel.Left;
        }

        var channel = ToChannel(
            selfUserId,
            channelReadModel,
            photoReadModel,
            channelMemberReadModel,
            channelMemberIsLeft, layer);

        if (channelReadModel.Broadcast)
        {
            if (selfUserId != channelReadModel.CreatorId)
            {
                chatAdminReadModels = [];
            }
        }

        var participants =
            ToChannelParticipantsCore(selfUserId, channelReadModel, chatAdminReadModels, channelMemberReadModels,
                layer);

        return new TChannelParticipants
        {
            Chats = new TVector<IChat>(channel),
            Count = participants.Count,
            Participants = [.. participants],
            Users = [.. users]
        };
    }

    public Schema.Messages.IChatFull ToChannelFull(
        long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelFullReadModel channelFullReadModel,
        IChannelMemberReadModel? channelMemberReadModel,
        IPeerNotifySettingsReadModel peerNotifySettingsReadModel,
        IChatInviteReadModel? chatInviteReadModel = null,
        int layer = 0
    )
    {
        var channel = ToChannel(selfUserId, channelReadModel, photoReadModel, channelMemberReadModel, null, layer);

        var fullChat = ToChannelFull(
            selfUserId,
            channelReadModel,
            photoReadModel,
            channelFullReadModel,
            peerNotifySettingsReadModel,
            chatInviteReadModel,
            layer
        );

        var chatFull = new TChatFull
        {
            Chats = new TVector<IChat>(channel),
            FullChat = fullChat,
            Users = []
        };

        return chatFull;
    }

    private IChat ToChannelCore(long selfUserId, IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel? channelMemberReadModel,
        bool? channelMemberIsLeft,
        int layer
    )
    {
        if (channelMemberReadModel is { Kicked: true })
        {
            return new TChannelForbidden
            {
                Broadcast = channelReadModel.Broadcast,
                AccessHash = channelReadModel.AccessHash,
                Id = channelReadModel.ChannelId,
                Title = channelReadModel.Title,
                Megagroup = channelReadModel.MegaGroup,
                UntilDate = channelMemberReadModel.UntilDate
            };
        }

        var channel = channelLayeredService.GetConverter(layer).ToChannel(channelReadModel);
        channel.Creator = channelReadModel.CreatorId == selfUserId;
        channel.Photo = photoLayeredService.GetConverter(layer).ToChatPhoto(photoReadModel);
        channel.EmojiStatus = emojiStatusLayeredService.GetConverter(layer).ToEmojiStatus(channelReadModel.EmojiStatus);
        channel.Left = false;

        if (channelMemberIsLeft.HasValue)
        {
            channel.Left = channelMemberIsLeft.Value;
        }
        else
        {
            if (channelMemberReadModel == null || channelMemberReadModel.Left)
            {
                channel.Left = true;
            }
        }

        if (channelMemberReadModel != null && channelMemberReadModel.BannedRights != 0)
        {
            var bannedRights = chatBannedRightsLayeredService.GetConverter(layer)
                .ToChatBannedRights(ChatBannedRights.FromValue(channelMemberReadModel.BannedRights,
                    channelMemberReadModel.UntilDate));
            channel.BannedRights = bannedRights;
        }

        if (channel.Creator)
        {
            channel.AdminRights = chatAdminRightsLayeredService.GetConverter(layer)
                .ToChatAdminRights(ChatAdminRights.GetCreatorRights());
            channel.Left = false;
        }
        else
        {
            var admin = channelReadModel.AdminList.FirstOrDefault(p => p.UserId == selfUserId);
            channel.AdminRights = admin != null
                ? chatAdminRightsLayeredService.GetConverter(layer)
                    .ToChatAdminRights(admin.AdminRights)
                : null;
        }

        return channel;
    }

    public IChatFull ToChannelFull(long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelFullReadModel channelFullReadModel,
        //IChannelMemberReadModel? channelMemberReadModel,
        IPeerNotifySettingsReadModel? peerNotifySettingsReadModel = null,
        IChatInviteReadModel? chatInviteReadModel = null,
        int layer = 0
    )
    {
        if (channelReadModel == null || channelFullReadModel == null)
        {
            throw new RpcException(RpcErrors.RpcErrors400.ChannelInvalid);
        }

        var channelFull = channelFullLayeredService.GetConverter(layer)
            .ToChannelFull(channelFullReadModel);
        channelFull.ChatPhoto = photoLayeredService.GetConverter(layer)
            .ToPhoto(photoReadModel);
        channelFull.NotifySettings = peerNotifySettingsLayeredService.GetConverter(layer)
            .ToPeerNotifySettings(peerNotifySettingsReadModel);
        channelFull.Pts = channelReadModel.Pts;
        channelFull.ParticipantsCount = channelReadModel.ParticipantsCount;
        channelFull.BotInfo = [];
        if (channelFullReadModel.RecentRequesters?.Count > 0 &&
            channelReadModel.AdminList.Any(p => p.UserId == selfUserId))
        {
            channelFull.RequestsPending = channelFullReadModel.RequestsPending;
            channelFull.RecentRequesters = [.. channelFullReadModel.RecentRequesters];
        }

        // Only creator and channel admin can view participants list for broadcast
        if (channelReadModel.Broadcast)
        {
            if (channelReadModel.CreatorId == selfUserId ||
                channelReadModel.AdminList.FirstOrDefault(p => p.UserId == selfUserId) != null)
            {
                channelFull.CanViewParticipants = true;
            }
            else
            {
                channelFull.CanViewParticipants = false;
            }
        }

        if (selfUserId == MyTelegramServerDomainConsts.LeftChannelUid)
        {
            channelFull.CanViewParticipants = false;
            channelFull.CanSetUsername = false;
        }

        if (channelReadModel.CreatorId == selfUserId)
        {
            channelFull.CanSetUsername = true;
            channelFull.CanDeleteChannel = true;
        }

        if (channelFull.SlowmodeSeconds > 0)
        {
            if (selfUserId != channelReadModel.CreatorId && selfUserId == channelReadModel.LastSenderPeerId)
            {
                var nextSendDate = channelReadModel.LastSendDate + channelFull.SlowmodeSeconds;
                channelFull.SlowmodeNextSendDate = nextSendDate;
            }
        }

        if (chatInviteReadModel != null && channelReadModel.AdminList.Any(p => p.UserId == selfUserId))
        {
            channelFull.ExportedInvite = chatInviteExportedConverterService.ToExportedChatInvite(chatInviteReadModel, layer);
        }

        if (channelFull.Call != null)
        {
            channelFull.GroupcallDefaultJoinAs = new TPeerUser
            {
                UserId = selfUserId
            };
        }

        if (!channelReadModel.Broadcast && channelReadModel.CreatorId == selfUserId)
        {
            channelFull.CanSetStickers = true;
        }

        return channelFull;
    }

    private IReadOnlyList<IChannelParticipant> ToChannelParticipantsCore(
        long selfUserId,
        IChannelReadModel channelReadModel,
        //IPhotoReadModel? photoReadModel,
        IReadOnlyCollection<IChatAdminReadModel>? chatAdminReadModels,
        IReadOnlyCollection<IChannelMemberReadModel> channelMemberReadModels,
        int layer
    )
    {
        var participants = new List<IChannelParticipant>();

        foreach (var chatAdminReadModel in chatAdminReadModels ?? [])
        {
            participants.Add(ToChatParticipantAdmin(selfUserId, chatAdminReadModel));
        }

        foreach (var channelMemberReadModel in channelMemberReadModels)
        {
            if (
                (channelReadModel.Broadcast || channelReadModel.HasLink) &&
                channelMemberReadModel.UserId == channelReadModel.CreatorId &&
                selfUserId != channelReadModel.CreatorId)
            {
                continue;
            }

            participants.Add(ToChannelParticipantCore(selfUserId, channelReadModel, channelMemberReadModel, layer));
        }

        return participants;
    }

    protected virtual IChannelParticipant ToChatParticipantAdmin(long selfUserId,
        IChatAdminReadModel chatAdminReadModel)
    {
        if (chatAdminReadModel.IsCreator)
        {
            return new TChannelParticipantCreator
            {
                AdminRights = ChatAdminRights.GetCreatorRights().ToChatAdminRights(),
                Rank = chatAdminReadModel.Rank,
                UserId = chatAdminReadModel.UserId
            };
        }

        return new TChannelParticipantAdmin
        {
            Date = chatAdminReadModel.Date,
            InviterId = chatAdminReadModel.PromotedBy,
            UserId = chatAdminReadModel.UserId,
            CanEdit = chatAdminReadModel.CanEdit,
            AdminRights = chatAdminReadModel.AdminRights.ToChatAdminRights(),
            PromotedBy = chatAdminReadModel.PromotedBy,
            Self = selfUserId == chatAdminReadModel.UserId,
            Rank = chatAdminReadModel.Rank
        };
    }

    private IChannelParticipant ToChannelParticipantCore(
        long selfUserId,
        IChannelReadModel channelReadModel,
        //IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel channelMemberReadModel,
        int layer
    )
    {
        var bannedRights = ChatBannedRights.FromValue(channelMemberReadModel.BannedRights,
            channelMemberReadModel.UntilDate).ToChatBannedRights();
        if (channelMemberReadModel.Kicked ||
            (channelMemberReadModel.BannedRights != 0 &&
             channelMemberReadModel.BannedRights != ChatBannedRights.CreateDefaultBannedRights().ToIntValue() &&
             !channelMemberReadModel.Left))
        {
            return new TChannelParticipantBanned
            {
                BannedRights = bannedRights,
                Date = channelMemberReadModel.Date,
                Peer = new TPeerUser { UserId = channelMemberReadModel.UserId },
                KickedBy = channelMemberReadModel.KickedBy,
                Left = false
            };
        }

        if (channelMemberReadModel.Left)
        {
            return new TChannelParticipantLeft { Peer = new TPeerUser { UserId = channelMemberReadModel.UserId } };
        }

        if (channelMemberReadModel.UserId == channelReadModel.CreatorId)
        {
            return new TChannelParticipantCreator
            {
                UserId = channelMemberReadModel.UserId,
                AdminRights = ChatAdminRights.GetCreatorRights().ToChatAdminRights()
            };
        }

        var admin = channelReadModel.AdminList.FirstOrDefault(p => p.UserId == channelMemberReadModel.UserId);
        if (admin != null)
        {
            return new TChannelParticipantAdmin
            {
                AdminRights = admin.AdminRights.ToChatAdminRights(),
                Date = channelMemberReadModel.Date,
                InviterId = channelMemberReadModel.InviterId,
                Rank = admin.Rank,
                UserId = admin.UserId,
                Self = channelMemberReadModel.UserId == selfUserId,
                CanEdit = admin.CanEdit,
                PromotedBy = admin.PromotedBy
            };
        }

        if (channelMemberReadModel.UserId == selfUserId)
        {
            return channelParticipantSelfLayeredService.GetConverter(layer)
                .ToChannelParticipantSelf(channelMemberReadModel);
        }

        return channelParticipantLayeredService.GetConverter(layer).ToChatParticipant(channelMemberReadModel);
    }
}