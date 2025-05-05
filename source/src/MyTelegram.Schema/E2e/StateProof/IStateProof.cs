// ReSharper disable All

namespace MyTelegram.Schema.E2e;


[JsonDerivedType(typeof(TStateProof), nameof(TStateProof))]
public interface IStateProof : IObject
{
    BitArray Flags { get; set; }
    byte[] KvHash { get; set; }
    MyTelegram.Schema.E2e.IGroupState? GroupState { get; set; }
    MyTelegram.Schema.E2e.ISharedKey? SharedKey { get; set; }
}
