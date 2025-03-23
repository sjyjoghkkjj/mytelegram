// ReSharper disable All

using MyTelegram.Schema.Payments;

namespace MyTelegram.Converters.TLObjects.Payments;

internal sealed class StarsStatusConverter : IStarsStatusConverter, ITransientDependency
{
    public int Layer => Layers.LayerLatest;

    public IStarsStatus ToStarsStatus()
    {
        return new TStarsStatus
        {
            Balance = new TStarsAmount(),
            Chats = [],
            History = [],
            Users = []
        };
    }
}
