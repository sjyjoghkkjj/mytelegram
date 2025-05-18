// ReSharper disable All

namespace MyTelegram.Schema.E2e;


[JsonDerivedType(typeof(TBlock), nameof(TBlock))]
public interface IBlock : IObject
{
    byte[] Signature { get; set; }
    BitArray Flags { get; set; }
    byte[] PrevBlockHash { get; set; }
    TVector<MyTelegram.Schema.E2e.IChange> Changes { get; set; }
    int Height { get; set; }
    MyTelegram.Schema.E2e.IStateProof StateProof { get; set; }
    byte[]? SignaturePublicKey { get; set; }
}
