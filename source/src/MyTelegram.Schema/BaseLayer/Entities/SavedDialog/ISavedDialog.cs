// ReSharper disable All

namespace MyTelegram.Schema;

///<summary>
/// Represents a <a href="https://corefork.telegram.org/api/saved-messages">saved message dialog »</a>.
/// See <a href="https://corefork.telegram.org/constructor/SavedDialog" />
///</summary>
[JsonDerivedType(typeof(TSavedDialog), nameof(TSavedDialog))]
[JsonDerivedType(typeof(TMonoForumDialog), nameof(TMonoForumDialog))]
public interface ISavedDialog : IObject
{
    ///<summary>
    /// Flags, see <a href="https://corefork.telegram.org/mtproto/TL-combinators#conditional-fields">TL conditional fields</a>
    ///</summary>
    BitArray Flags { get; set; }
    
    ///<summary>
    /// The dialog
    /// See <a href="https://corefork.telegram.org/type/Peer" />
    ///</summary>
    MyTelegram.Schema.IPeer Peer { get; set; }

    ///<summary>
    /// The latest message ID
    ///</summary>
    int TopMessage { get; set; }
}
