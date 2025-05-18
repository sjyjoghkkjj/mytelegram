// ReSharper disable All

namespace MyTelegram.Schema.E2e;


[JsonDerivedType(typeof(THandshakePrivateAccept), "THandshakePrivateAcceptLayer0")]
[JsonDerivedType(typeof(THandshakePrivateFinish), "THandshakePrivateFinishLayer0")]
public interface IHandshakePrivate : IObject
{
    byte[] AlicePK { get; set; }
    byte[] BobPK { get; set; }
    long AliceUserId { get; set; }
    long BobUserId { get; set; }
    byte[] AliceNonce { get; set; }
    byte[] BobNonce { get; set; }
}
