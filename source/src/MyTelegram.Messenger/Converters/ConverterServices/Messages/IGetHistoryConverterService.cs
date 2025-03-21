namespace MyTelegram.Messenger.Converters.ConverterServices.Messages;

public interface IGetHistoryConverterService
{
    IMessages ToMessages(GetMessageOutput output, int layer);
}