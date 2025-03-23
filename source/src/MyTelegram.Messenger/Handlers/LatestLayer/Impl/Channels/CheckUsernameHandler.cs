namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Channels;

///<summary>
/// Check if a username is free and can be assigned to a channel/supergroup
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNELS_ADMIN_PUBLIC_TOO_MUCH You're admin of too many public channels, make some channels private to change the username of this channel.
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 400 CHAT_ID_INVALID The provided chat id is invalid.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 400 USERNAME_INVALID The provided username is not valid.
/// 400 USERNAME_OCCUPIED The provided username is already occupied.
/// 400 USERNAME_PURCHASE_AVAILABLE The specified username can be purchased on <a href="https://fragment.com/">https://fragment.com</a>.
/// See <a href="https://corefork.telegram.org/method/channels.checkUsername" />
///</summary>
internal sealed class CheckUsernameHandler(
    IQueryProcessor queryProcessor,
    IAccessHashHelper accessHashHelper)
    : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestCheckUsername, IBool>,
        Channels.ICheckUsernameHandler
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestCheckUsername obj)
    {
        switch (obj.Channel)
        {
            case TInputChannel inputChannel1:
                await accessHashHelper.CheckAccessHashAsync(inputChannel1.ChannelId, inputChannel1.AccessHash);
                break;
            case TInputChannelEmpty _:
                break;
        }

        if (string.IsNullOrEmpty(obj.Username) ||
            obj.Username.Length < MyTelegramServerDomainConsts.UsernameMinLength ||
            obj.Username.Length > MyTelegramServerDomainConsts.UsernameMaxLength
           )
        {
            RpcErrors.RpcErrors400.UsernameInvalid.ThrowRpcError();
        }

        var item = await queryProcessor
            .ProcessAsync(new GetUserNameByIdQuery(obj.Username.ToLower()));
        if (item == null)
        {
            return new TBoolTrue();
        }

        return new TBoolFalse();
    }
}
