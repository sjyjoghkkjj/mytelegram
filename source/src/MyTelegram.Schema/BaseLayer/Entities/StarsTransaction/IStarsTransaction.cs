// ReSharper disable All

namespace MyTelegram.Schema;

///<summary>
/// Represents a <a href="https://corefork.telegram.org/api/stars">Telegram Stars transaction »</a>.
/// See <a href="https://corefork.telegram.org/constructor/StarsTransaction" />
///</summary>
[JsonDerivedType(typeof(TStarsTransaction), nameof(TStarsTransaction))]
public interface IStarsTransaction : IObject
{
    ///<summary>
    /// Flags, see <a href="https://corefork.telegram.org/mtproto/TL-combinators#conditional-fields">TL conditional fields</a>
    ///</summary>
    BitArray Flags { get; set; }

    ///<summary>
    /// Whether this transaction is a refund.
    ///</summary>
    bool Refund { get; set; }

    ///<summary>
    /// Transaction ID.
    ///</summary>
    string Id { get; set; }

    ///<summary>
    /// Date of the transaction (unixtime).
    ///</summary>
    int Date { get; set; }

    ///<summary>
    /// Source of the incoming transaction, or its recipient for outgoing transactions.
    /// See <a href="https://corefork.telegram.org/type/StarsTransactionPeer" />
    ///</summary>
    MyTelegram.Schema.IStarsTransactionPeer Peer { get; set; }

    ///<summary>
    /// For transactions with bots, title of the bought product.
    ///</summary>
    string? Title { get; set; }

    ///<summary>
    /// For transactions with bots, description of the bought product.
    ///</summary>
    string? Description { get; set; }

    ///<summary>
    /// For transactions with bots, photo of the bought product.
    /// See <a href="https://corefork.telegram.org/type/WebDocument" />
    ///</summary>
    MyTelegram.Schema.IWebDocument? Photo { get; set; }
}
