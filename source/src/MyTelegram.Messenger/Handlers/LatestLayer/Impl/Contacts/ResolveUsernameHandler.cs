// ReSharper disable All

namespace MyTelegram.Handlers.Contacts;

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
    IUserAppService userAppService,
    IChannelAppService channelAppService,
    ILayeredService<IChatConverter> layeredChatService,
    ILayeredService<IUserConverter> layeredUserService,
    IPhotoAppService photoAppService,
    IPrivacyAppService privacyAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Contacts.RequestResolveUsername,
            MyTelegram.Schema.Contacts.IResolvedPeer>,
        Contacts.IResolveUsernameHandler
{
    protected override async Task<IResolvedPeer> HandleCoreAsync(IRequestInput input,
        RequestResolveUsername obj)
    {
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
                        var userReadModel = await userAppService.GetAsync(userNameReadModel.PeerId);

                        if (userReadModel != null)
                        {
                            var contactReadModel =
                                await queryProcessor.ProcessAsync(
                                    new GetContactQuery(input.UserId, userReadModel.UserId), default);
                            var photos = await photoAppService.GetPhotosAsync(userReadModel, contactReadModel);
                            var privacies = await privacyAppService.GetPrivacyListAsync(userReadModel!.UserId);

                            return new TResolvedPeer
                            {
                                Chats = new TVector<IChat>(),
                                Peer = new TPeerUser { UserId = userNameReadModel.PeerId },
                                Users = new TVector<IUser>(layeredUserService.GetConverter(input.Layer).ToUser(
                                    input.UserId,
                                    userReadModel,
                                    photos,
                                    contactReadModel,
                                    privacies))
                            };
                        }

                        break;
                    case PeerType.Chat:
                        {
                            var chatReadModel = await queryProcessor
                                .ProcessAsync(new GetChatByChatIdQuery(userNameReadModel.PeerId), default)
                         ;
                            if (chatReadModel != null)
                            {
                                var photoReadModel = await photoAppService.GetAsync(chatReadModel.PhotoId);
                                return new TResolvedPeer
                                {
                                    Chats = new TVector<IChat>(layeredChatService.GetConverter(input.Layer).ToChat(input.UserId, chatReadModel, photoReadModel)),
                                    Peer = new TPeerChat { ChatId = userNameReadModel.PeerId },
                                    Users = new TVector<IUser>()
                                };
                            }
                        }
                        break;
                    case PeerType.Channel:
                        {
                            var channelReadModel = await channelAppService.GetAsync(userNameReadModel.PeerId);
                            if (channelReadModel != null)
                            {
                                var photoReadModel = await photoAppService.GetAsync(channelReadModel.PhotoId);
                                var channelMemberReadModel = await queryProcessor.ProcessAsync(new GetChannelMemberByUserIdQuery(channelReadModel.ChannelId, input.UserId));

                                return new TResolvedPeer
                                {
                                    Chats =
                                        new TVector<IChat>(layeredChatService.GetConverter(input.Layer).ToChannel(
                                            input.UserId,
                                            channelReadModel,
                                            photoReadModel,
                                            channelMemberReadModel, channelMemberReadModel?.Left ?? true)),
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
