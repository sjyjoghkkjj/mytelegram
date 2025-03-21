namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Get all groups that can be used as <a href="https://corefork.telegram.org/api/discussion">discussion groups</a>.Returned <a href="https://corefork.telegram.org/api/channel#basic-groups">basic group chats</a> must be first upgraded to <a href="https://corefork.telegram.org/api/channel#supergroups">supergroups</a> before they can be set as a discussion group.<br>
/// To set a returned supergroup as a discussion group, access to its old messages must be enabled using <a href="https://corefork.telegram.org/method/channels.togglePreHistoryHidden">channels.togglePreHistoryHidden</a>, first.
/// See <a href="https://corefork.telegram.org/method/channels.getGroupsForDiscussion" />
///</summary>
internal sealed class GetGroupsForDiscussionHandler(
    IQueryProcessor queryProcessor,
    IChatConverterService chatConverterService,
    ILayeredService<IPhotoConverter> layeredPhotoService,
    IPhotoAppService photoAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestGetGroupsForDiscussion,
            MyTelegram.Schema.Messages.IChats>,
        IGetGroupsForDiscussionHandler
{
    private readonly ILayeredService<IPhotoConverter> _layeredPhotoService = layeredPhotoService;

    protected override async Task<MyTelegram.Schema.Messages.IChats> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestGetGroupsForDiscussion obj)
    {
        var channelReadModels = await queryProcessor.ProcessAsync(new GetMegaGroupByUserIdQuery(input.UserId));
        var photoReadModels = await photoAppService.GetPhotosAsync(channelReadModels);

        var channelList = chatConverterService.ToChannelList(
            input.UserId,
            channelReadModels,
            photoReadModels,
            [],
            channelReadModels.Select(p => p.ChannelId).ToList(),
            true,
            input.Layer
            );

        return new TChats { Chats = new TVector<IChat>(channelList) };
    }
}
