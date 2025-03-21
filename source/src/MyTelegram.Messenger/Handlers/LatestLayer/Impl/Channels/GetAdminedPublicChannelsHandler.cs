namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

/// <summary>
///     Get <a href="https://corefork.telegram.org/api/channel">channels/supergroups/geogroups</a> we're admin in. Usually
///     called when the user exceeds the <a href="https://corefork.telegram.org/constructor/config">limit</a> for owned
///     public <a href="https://corefork.telegram.org/api/channel">channels/supergroups/geogroups</a>, and the user is
///     given the choice to remove one of his channels/supergroups/geogroups.
///     <para>Possible errors</para>
///     Code Type Description
///     400 CHANNELS_ADMIN_LOCATED_TOO_MUCH The user has reached the limit of public geogroups.
///     400 CHANNELS_ADMIN_PUBLIC_TOO_MUCH You're admin of too many public channels, make some channels private to change
///     the username of this channel.
///     See <a href="https://corefork.telegram.org/method/channels.getAdminedPublicChannels" />
/// </summary>
internal sealed class GetAdminedPublicChannelsHandler(
    IQueryProcessor queryProcessor,
    IChatConverterService chatConverterService)
    : RpcResultObjectHandler<Schema.Channels.RequestGetAdminedPublicChannels, Schema.Messages.IChats>,
        Channels.IGetAdminedPublicChannelsHandler
{
    protected override async Task<Schema.Messages.IChats> HandleCoreAsync(IRequestInput input,
        Schema.Channels.RequestGetAdminedPublicChannels obj)
    {
        var channelReadModels = await queryProcessor.ProcessAsync(new GetAdminedPublicChannelsQuery(input.UserId));
        var channelIds = channelReadModels.Select(p => p.ChannelId).ToList();
        var channels = await chatConverterService.GetChannelListAsync(input.UserId, channelIds, false, null, channelIds,
            layer: input.Layer);

        return new TChats
        {
            Chats = new TVector<IChat>(channels)
        };
    }
}