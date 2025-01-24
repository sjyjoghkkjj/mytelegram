// ReSharper disable All

namespace MyTelegram.Handlers.Messages;

/// <summary>
///     Creates a new chat.May also return 0-N updates of type
///     <a href="https://corefork.telegram.org/constructor/updateGroupInvitePrivacyForbidden">updateGroupInvitePrivacyForbidden</a>
///     : it indicates we couldn't add a user to a chat because of their privacy settings; if required, an
///     <a href="https://corefork.telegram.org/api/invites">invite link</a> can be shared with the user, instead.
///     <para>Possible errors</para>
///     Code Type Description
///     500 CHAT_ID_GENERATE_FAILED Failure while generating the chat ID.
///     400 CHAT_INVALID Invalid chat.
///     400 CHAT_TITLE_EMPTY No chat title provided.
///     400 INPUT_USER_DEACTIVATED The specified user was deleted.
///     400 USERS_TOO_FEW Not enough users (to create a chat, for example).
///     406 USER_RESTRICTED You're spamreported, you can't create channels or chats.
///     See <a href="https://corefork.telegram.org/method/messages.createChat" />
/// </summary>
internal sealed class CreateChatHandler(
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IRandomHelper randomHelper,
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IPrivacyAppService privacyAppService,
    IOptions<MyTelegramMessengerServerOptions> options)
    : RpcResultObjectHandler<Schema.Messages.RequestCreateChat, Schema.Messages.IInvitedUsers>,
        Messages.ICreateChatHandler
{
    protected override async Task<Schema.Messages.IInvitedUsers> HandleCoreAsync(IRequestInput input,
        RequestCreateChat obj)
    {
        var memberUserIdList = new List<long>();
        //var userIdList = new List<long>();
        var botList = new List<long>();
        foreach (var inputUser in obj.Users)
        {
            if (inputUser is TInputUser u)
            {
                await accessHashHelper.CheckAccessHashAsync(u.UserId, u.AccessHash);
                memberUserIdList.Add(u.UserId);
                //userIdList.Add(u.UserId);

                if (peerHelper.IsBotUser(u.UserId))
                {
                    botList.Add(u.UserId);
                }
            }
        }

        memberUserIdList = memberUserIdList.Distinct().ToList();

        if (options.Value.AutoCreateSuperGroup)
        {
            var channelId = await idGenerator.NextLongIdAsync(IdType.ChannelId);
            var accessHash = randomHelper.NextInt64();
            var date = DateTime.UtcNow.ToTimestamp();
            var createChannelCommand = new CreateChannelCommand(ChannelId.Create(channelId),
                input.ToRequestInfo(),
                channelId,
                input.UserId,
                obj.Title,
                //obj.Broadcast,
                false,
                true,
                string.Empty,
                string.Empty,
                accessHash,
                date,
                randomHelper.NextInt64(),
                new TMessageActionChannelCreate { Title = obj.Title }.ToBytes().ToHexString(),
                null,
                false,
                null,
                null,
                null,
                true
            );
            await commandBus.PublishAsync(createChannelCommand, CancellationToken.None);

            var privacyRestrictedUserIdList = new List<long>();
            await privacyAppService.ApplyPrivacyListAsync(input.UserId, memberUserIdList,
                privacyRestrictedUserIdList.Add, new List<PrivacyType>
                {
                    PrivacyType.ChatInvite
                });
            memberUserIdList.RemoveAll(privacyRestrictedUserIdList.Contains);

            // all selected users are rejected to be added to chat or channel

            var command = new StartInviteToChannelCommand(
                ChannelId.Create(channelId),
                input.ToRequestInfo(),
                channelId,
                input.UserId,
                1, //default maxMessageId 1
                memberUserIdList,
                privacyRestrictedUserIdList,
                botList,
                CurrentDate,
                randomHelper.NextInt64(),
                new TMessageActionChatAddUser { Users = new TVector<long>(memberUserIdList) }.ToBytes().ToHexString(),
                ChatJoinType.InvitedByAdmin
            );
            await commandBus.PublishAsync(command);
        }
        else
        {
            var chatId = await idGenerator.NextLongIdAsync(IdType.ChatId);
            var randomId = randomHelper.NextInt64();
            var messageActionData =
                new TMessageActionChatCreate { Title = obj.Title, Users = new TVector<long>(memberUserIdList) }
                    .ToBytes()
                    .ToHexString();

            var command = new CreateChatCommand(ChatId.Create(chatId),
                input.ToRequestInfo(),
                chatId,
                input.UserId,
                obj.Title,
                memberUserIdList,
                CurrentDate,
                randomId,
                messageActionData,
                obj.TtlPeriod
            );
            await commandBus.PublishAsync(command);
        }

        return null!;
    }
}