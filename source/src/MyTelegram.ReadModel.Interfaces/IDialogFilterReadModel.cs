namespace MyTelegram.ReadModel.Interfaces;

public interface IDialogFilterReadModel : IReadModel
{
    long OwnerUserId { get; }
    DialogFilter Filter { get; }
}
