using MyTelegram.Messenger.Converters.ConverterServices;

namespace MyTelegram.Messenger.Converters.ConverterServices.Contacts;

public interface ISearchConverterService
{
    IFound ToFound(SearchContactOutput output, int layer);
}

internal sealed class SearchConverterService(IUserConverterService userConverterService,
    IChatConverterService chatConverterService
    ) : ISearchConverterService, ITransientDependency
{
    public IFound ToFound(SearchContactOutput output, int layer)
    {
        var users = userConverterService.ToUserList(output.SelfUserId,
            output.UserList,
            output.PhotoList,
            output.ContactList,
            output.PrivacyList,
            layer);

        var myChannels = chatConverterService.ToChannelList(output.SelfUserId,
            output.MyChannelList,
            output.PhotoList,
            output.ChannelMemberList,
            output.MyChannelList.Select(p => p.ChannelId).ToList(),
            false,
            layer);

        var otherChannels = chatConverterService.ToChannelList(output.SelfUserId,
            output.ChannelList,
            output.PhotoList,
            [],
            [],
            false,
            layer);

        var peers = output.UserList.Select(p => (IPeer)new TPeerUser { UserId = p.UserId }).ToList();
        peers.AddRange(output.MyChannelList.Select(p => (IPeer)new TPeerChannel { ChannelId = p.ChannelId }));

        var otherPeers = output.ChannelList.Select(p => (IPeer)new TPeerChannel { ChannelId = p.ChannelId });

        return new TFound
        {
            Chats = new TVector<IChat>(myChannels),
            MyResults = new TVector<IPeer>(peers),
            Results = new TVector<IPeer>(otherPeers),
            Users = new TVector<IUser>(users)
        };
    }
}
