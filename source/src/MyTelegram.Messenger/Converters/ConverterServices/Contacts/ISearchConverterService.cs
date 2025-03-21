namespace MyTelegram.Messenger.Converters.ConverterServices.Contacts;

public interface ISearchConverterService
{
    IFound ToFound(SearchContactOutput output, int layer);
}