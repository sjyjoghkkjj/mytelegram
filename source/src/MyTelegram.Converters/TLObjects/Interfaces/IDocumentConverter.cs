namespace MyTelegram.Converters.TLObjects.Interfaces;

public interface IDocumentConverter : ILayeredConverter
{
    IDocument ToDocument(IDocumentReadModel documentReadModel);
}