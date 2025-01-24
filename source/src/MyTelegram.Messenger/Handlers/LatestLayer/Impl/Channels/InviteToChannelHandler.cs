// ReSharper disable All

namespace MyTelegram.Handlers.Channels;

///<summary>
/// Invite users to a channel/supergroupMay also return 0-N updates of type <a href="https://corefork.telegram.org/constructor/updateGroupInvitePrivacyForbidden">updateGroupInvitePrivacyForbidden</a>: it indicates we couldn't add a user to a chat because of their privacy settings; if required, an <a href="https://corefork.telegram.org/api/invites">invite link</a> can be shared with the user, instead.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 BOTS_TOO_MUCH There are too many bots in this chat/channel.
/// 400 BOT_GROUPS_BLOCKED This bot can't be added to groups.
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 406 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 403 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 400 CHAT_INVALID Invalid chat.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 INPUT_USER_DEACTIVATED The specified user was deleted.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 USERS_TOO_MUCH The maximum number of users has been exceeded (to create a chat, for example).
/// 400 USER_BANNED_IN_CHANNEL You're banned from sending messages in supergroups/channels.
/// 400 USER_BLOCKED User blocked.
/// 400 USER_BOT Bots can only be admins in channels.
/// 403 USER_CHANNELS_TOO_MUCH One of the users you tried to add is already in too many channels/supergroups.
/// 400 USER_ID_INVALID The provided user ID is invalid.
/// 400 USER_KICKED This user was kicked from this supergroup/channel.
/// 403 USER_NOT_MUTUAL_CONTACT The provided user is not a mutual contact.
/// 403 USER_PRIVACY_RESTRICTED The user's privacy settings do not allow you to do this.
/// See <a href="https://corefork.telegram.org/method/channels.inviteToChannel" />
///</summary>
internal sealed class InviteToChannelHandler(
    ICommandBus commandBus,
    IRandomHelper randomHelper,
    IPeerHelper peerHelper,
    IChannelAppService channelAppService,
    IAccessHashHelper accessHashHelper,
    IPrivacyAppService privacyAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestInviteToChannel,
            MyTelegram.Schema.Messages.IInvitedUsers>,
        Channels.IInviteToChannelHandler
{
    protected override async Task<MyTelegram.Schema.Messages.IInvitedUsers> HandleCoreAsync(IRequestInput input,
        RequestInviteToChannel obj)
    {
        if (obj.Channel is TInputChannel inputChannel)
        {
            await accessHashHelper.CheckAccessHashAsync(inputChannel.ChannelId, inputChannel.AccessHash);
            var channelReadModel = await channelAppService.GetAsync(inputChannel.ChannelId);
            channelReadModel.ThrowExceptionIfChannelDeleted();

            if (obj.Users.Count == 1)
            {
                if (obj.Users[0] is TInputUser inputUser)
                {
                    await privacyAppService.ApplyPrivacyAsync(input.UserId,
                        inputUser.UserId,
                        () => RpcErrors.RpcErrors403.UserPrivacyRestricted.ThrowRpcError(),
                        new List<PrivacyType>()
                        {
                            PrivacyType.ChatInvite
                        }
                    );
                }
            }

            var userIdList = new List<long>();
            var botList = new List<long>();
            foreach (TInputUser inputUser in obj.Users)
            {
                userIdList.Add(inputUser.UserId);
                if (peerHelper.IsBotUser(inputUser.UserId))
                {
                    botList.Add(inputUser.UserId);
                }
            }

            var privacyRestrictedUserIdList = new List<long>();
            await privacyAppService.ApplyPrivacyListAsync(input.UserId, userIdList,
                 privacyRestrictedUserIdList.Add, new List<PrivacyType>
                {
                    PrivacyType.ChatInvite
                });
            userIdList.RemoveAll(privacyRestrictedUserIdList.Contains);

            var inviterUserId = input.UserId;
            if (channelReadModel!.Broadcast || channelReadModel.HasLink)
            {
                inviterUserId = MyTelegramServerDomainConsts.GroupAnonymousBotUserId;
            }

            var command = new StartInviteToChannelCommand(
                ChannelId.Create(inputChannel.ChannelId),
                input.ToRequestInfo(),
                inputChannel.ChannelId,
                inviterUserId,
                channelReadModel!.TopMessageId,
                userIdList,
                privacyRestrictedUserIdList,
                botList,
                CurrentDate,
                randomHelper.NextInt64(),
                new TMessageActionChatAddUser { Users = new TVector<long>(userIdList) }.ToBytes().ToHexString(),
                ChatJoinType.InvitedByAdmin
                );
            await commandBus.PublishAsync(command);

            return null!;
        }

        throw new NotImplementedException();
    }
}
