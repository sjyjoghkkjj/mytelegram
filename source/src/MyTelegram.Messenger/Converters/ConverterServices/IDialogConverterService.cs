namespace MyTelegram.Messenger.Converters.ConverterServices;

public interface IDialogConverterService
{
    IDialogs ToDialogs(GetDialogOutput output, int layer = 0);
    IPeerDialogs ToPeerDialogs(GetDialogOutput output, int layer = 0);
}