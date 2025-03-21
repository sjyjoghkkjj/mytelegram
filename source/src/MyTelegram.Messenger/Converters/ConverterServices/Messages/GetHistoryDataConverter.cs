namespace MyTelegram.Messenger.Converters.ConverterServices.Messages;

public interface IGetHistoryConverterService
{
    IMessages ToMessages(GetMessageOutput output, int layer);
}

internal sealed class GetHistoryConverterService(IUserConverterService userConverterService, IChatConverterService chatConverterService,
    IMessageConverterService messageConverterService
    ) : IGetHistoryConverterService, ITransientDependency
{
    public IMessages ToMessages(GetMessageOutput output, int layer)
    {
        var messages = messageConverterService.ToMessageList(output.SelfUserId,
            output.MessageList,
            output.PollList,
            output.ChosenPollOptions,
            layer);

        var users = userConverterService.ToUserList(output.SelfUserId,
            output.UserList,
            output.PhotoList,
            output.ContactList,
            output.PrivacyList,
            layer);

        var channels = chatConverterService.ToChannelList(output.SelfUserId,
            output.ChannelList,
            output.PhotoList,
            output.ChannelMemberList,
            output.JoinedChannelIdList,
            false,
            layer);

        var hasMessages = messages.Count > 0;

        var offsetId = hasMessages ? output.MessageList.Max(p => p.MessageId) : 0;
        if (output.OffsetInfo?.LoadType == LoadType.Backward)
        {
            offsetId = hasMessages ? output.MessageList.Min(p => p.MessageId) : 0;
        }

        if (hasMessages && output.MessageList.All(p => p.ToPeerType == PeerType.Channel) && !output.IsSearchGlobal)
        {
            var channelPts = output.ChannelList.FirstOrDefault()?.Pts ?? output.Pts;

            return new TChannelMessages
            {
                Chats = new TVector<IChat>(channels),
                Messages = new TVector<IMessage>(messages),
                Users = new TVector<IUser>(users),
                Pts = channelPts,
                Count = messages.Count,
                OffsetIdOffset = offsetId,
                Topics = []
            };
        }

        if (messages.Count == output.Limit)
        {
            return new TMessagesSlice
            {
                Chats = new TVector<IChat>(channels),
                Count = messages.Count,
                Inexact = true,
                NextRate = DateTime.UtcNow.AddSeconds(3).ToTimestamp(),
                Messages = new TVector<IMessage>(messages),
                Users = new TVector<IUser>(users),
                OffsetIdOffset = offsetId,
            };
        }

        return new TMessages
        {
            Chats = new TVector<IChat>(channels),
            Messages = new TVector<IMessage>(messages),
            Users = new TVector<IUser>(users)
        };
    }
}
