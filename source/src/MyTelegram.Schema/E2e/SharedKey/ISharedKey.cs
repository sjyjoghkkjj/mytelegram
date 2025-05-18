// ReSharper disable All

namespace MyTelegram.Schema.E2e;


[JsonDerivedType(typeof(TSharedKey), nameof(TSharedKey))]
public interface ISharedKey : IObject
{
    byte[] Ek { get; set; }
    string EncryptedSharedKey { get; set; }
    TVector<long> DestUserId { get; set; }
    TVector<byte[]> DestHeader { get; set; }
}
