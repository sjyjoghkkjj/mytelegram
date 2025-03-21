using IChatFull = MyTelegram.Schema.IChatFull;

namespace MyTelegram.Messenger.Converters.ConverterServices;

public interface IChatConverterService
{
    Task<IChat> GetChannelAsync(long selfUserId, long channelId,
        //IChannelMemberReadModel? channelMemberReadModel,
        bool checkChannelMember,
        bool channelMemberIsLeft,
        int layer = 0
    );

    Task<List<IChat>> GetChannelListAsync(long selfUserId,
        List<long> channelIds,
        bool setIsLeftViaJoinedChannels,
        IReadOnlyCollection<IChannelMemberReadModel>? channelMemberReadModels = null,
        IReadOnlyCollection<long>? joinedChannelIds = null,
        bool resetLeftToFalse = false,
        int layer = 0);

    Task<IChatFull> GetChannelFullAsync(long selfUserId,
        long channelId,
        IPeerNotifySettingsReadModel? peerNotifySettingsReadModel = null,
        IChatInviteReadModel? chatInviteReadModel = null,
        int layer = 0);

    //Schema.IChannelParticipant ToChannelParticipant(
    //    long selfUserId,
    //    IChannelReadModel channelReadModel,
    //    IChannelMemberReadModel channelMemberReadModel,
    //    int layer = 0
    //    );

    Schema.Channels.IChannelParticipant ToChannelParticipant(
        long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel channelMemberReadModel,
        //IChatPhoto chatPhoto,
        IUser user,
        int layer = 0
    );

    //IChatFull ToChannelFull(long selfUserId,
    //    IChannelReadModel channelReadModel,
    //    IPhotoReadModel? photoReadModel,
    //    IChannelFullReadModel channelFullReadModel,
    //    //IChannelMemberReadModel? channelMemberReadModel,
    //    IPeerNotifySettingsReadModel? peerNotifySettingsReadModel = null,
    //    IChatInviteReadModel? chatInviteReadModel = null,
    //    int layer = 0
    //);

    IChat ToChannel(long selfUserId, IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelMemberReadModel? channelMemberReadModel,
        bool channelMemberIsLeft,
        int layer);

    List<IChat> ToChannelList(long selfUserId, IReadOnlyCollection<IChannelReadModel> channelReadModels,
        IReadOnlyCollection<IPhotoReadModel> photoReadModels,
        IReadOnlyCollection<IChannelMemberReadModel>? channelMemberReadModels,
        IReadOnlyCollection<long>? joinedChannelIds = null,
        bool resetLeftToFalse = false,
        int layer = 0);

    IChannelParticipants ToChannelParticipants(
        long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IReadOnlyCollection<IChatAdminReadModel>? chatAdminReadModels,
        IReadOnlyCollection<IChannelMemberReadModel> channelMemberReadModels,
        IEnumerable<IUser> users,
        DeviceType deviceType,
        bool forceNotLeft,
        int layer
    );

    Schema.Messages.IChatFull ToChannelFull(
        long selfUserId,
        IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel,
        IChannelFullReadModel channelFullReadModel,
        IChannelMemberReadModel? channelMemberReadModel,
        IPeerNotifySettingsReadModel peerNotifySettingsReadModel,
        IChatInviteReadModel? chatInviteReadModel = null,
        int layer = 0
    );
}