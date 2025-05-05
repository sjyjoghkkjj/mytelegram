// ReSharper disable All

namespace MyTelegram.Schema.E2e;


[JsonDerivedType(typeof(TGroupBroadcastNonceCommit), "TGroupBroadcastNonceCommitLayer0")]
[JsonDerivedType(typeof(TGroupBroadcastNonceReveal), "TGroupBroadcastNonceRevealLayer0")]
public interface IGroupBroadcast : IObject
{
    byte[] Signature { get; set; }
    long UserId { get; set; }
    int ChainHeight { get; set; }
    byte[] ChainHash { get; set; }
}
