using IExportedChatInvite = MyTelegram.Schema.IExportedChatInvite;

namespace MyTelegram.Messenger.Converters.TLObjects.Interfaces;
public interface IChatInviteExportedConverter : ILayeredConverter
{
    IExportedChatInvite ToExportedChatInvite(IChatInviteReadModel readModel);
    IExportedChatInvite ToExportedChatInvite(ChatInviteCreatedEvent source);
    IExportedChatInvite ToExportedChatInvite(ChatInviteEditedEvent source);
}