namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Contacts;

///<summary>
/// Resolve a @username to get peer info
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CONNECTION_LAYER_INVALID Layer invalid.
/// 400 USERNAME_INVALID The provided username is not valid.
/// 400 USERNAME_NOT_OCCUPIED The provided username is not occupied.
/// See <a href="https://corefork.telegram.org/method/contacts.resolveUsername" />
///</summary>
internal sealed class ResolveUsernameHandler(
    IQueryProcessor queryProcessor,
    //ILayeredService<IChatConverter> layeredChatService,
    //ILayeredService<IUserConverter> layeredUserService,
    IChatConverterService chatConverterService,
    IUserConverterService userConverterService,
    IChannelAppService channelAppService,
    IPhotoAppService photoAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Contacts.RequestResolveUsername,
            MyTelegram.Schema.Contacts.IResolvedPeer>,
       MyTelegram.Messenger.Handlers.LatestLayer.Impl.Contacts.IResolveUsernameHandler
{
    protected override async Task<IResolvedPeer> HandleCoreAsync(IRequestInput input,
        RequestResolveUsername obj)
    {
        //Console.WriteLine($"RequestResolveUsername:{obj.Username}");
        if (!string.IsNullOrEmpty(obj.Username))
        {
            var userNameReadModel = await queryProcessor
                .ProcessAsync(new GetUserNameByNameQuery(obj.Username), default)
         ;
            if (userNameReadModel != null)
            {
                switch (userNameReadModel.PeerType)
                {
                    case PeerType.User:
                        var user = await userConverterService.GetUserAsync(input.UserId, userNameReadModel.PeerId, false, false, input.Layer);
                        return new TResolvedPeer
                        {
                            Chats = [],
                            Peer = new TPeerUser { UserId = userNameReadModel.PeerId },
                            Users = new TVector<IUser>(user)
                        };

                    case PeerType.Channel:
                        {
                            var channelReadModel = await channelAppService.GetAsync(userNameReadModel.PeerId);

                            if (channelReadModel != null)
                            {
                                var photoReadModel = await photoAppService.GetAsync(channelReadModel.PhotoId ?? 0);
                                var channelMemberReadModel = await queryProcessor.ProcessAsync(new GetChannelMemberByUserIdQuery(channelReadModel.ChannelId, input.UserId));
                                return new TResolvedPeer
                                {
                                    Chats =
                                        new TVector<IChat>(chatConverterService.ToChannel(
                                            input.UserId,
                                            channelReadModel,
                                            photoReadModel,
                                            channelMemberReadModel, channelMemberReadModel?.Left ?? true, input.Layer)),
                                    Peer = new TPeerChannel { ChannelId = userNameReadModel.PeerId },
                                    Users = new TVector<IUser>()
                                };
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        RpcErrors.RpcErrors400.UsernameNotOccupied.ThrowRpcError();

        return null!;
    }
}
