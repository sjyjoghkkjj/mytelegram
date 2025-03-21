// ReSharper disable All

namespace MyTelegram.Schema;

///<summary>
/// Represents a <a href="https://corefork.telegram.org/api/gifts">gift</a>, displayed on a user's profile page.
/// See <a href="https://corefork.telegram.org/constructor/UserStarGift" />
///</summary>
public interface IUserStarGift : IObject
{
    ///<summary>
    /// Flags, see <a href="https://corefork.telegram.org/mtproto/TL-combinators#conditional-fields">TL conditional fields</a>
    ///</summary>
    BitArray Flags { get; set; }

    ///<summary>
    /// If set, <code>from_id</code> will not be visible to users (it will still be visible to the receiver of the gift).
    ///</summary>
    bool NameHidden { get; set; }

    ///<summary>
    /// If set, indicates this is a gift sent by <code>from_id</code>, received by the current user and currently hidden from our profile page.
    ///</summary>
    bool Unsaved { get; set; }

    ///<summary>
    /// Sender of the gift (may be empty for anonymous senders; will always be set if this gift was sent to us).
    ///</summary>
    long? FromId { get; set; }

    ///<summary>
    /// When was this gift sent.
    ///</summary>
    int Date { get; set; }

    ///<summary>
    /// The gift.
    /// See <a href="https://corefork.telegram.org/type/StarGift" />
    ///</summary>
    MyTelegram.Schema.IStarGift Gift { get; set; }

    ///<summary>
    /// Message attached to the gift by the sender.
    /// See <a href="https://corefork.telegram.org/type/TextWithEntities" />
    ///</summary>
    MyTelegram.Schema.ITextWithEntities? Message { get; set; }

    ///<summary>
    /// Only visible to the receiver of the gift, contains the ID of the <a href="https://corefork.telegram.org/constructor/messageService">messageService</a> with the <a href="https://corefork.telegram.org/constructor/messageActionStarGift">messageActionStarGift</a> in the chat with <code>from_id</code>.
    ///</summary>
    int? MsgId { get; set; }

    ///<summary>
    /// The receiver of this gift may convert it to this many Telegram Stars, instead of displaying it on their profile page.<br><code>convert_stars</code> will be equal to the buying price of the gift only if the gift was bought using recently bought Telegram Stars, otherwise it will be less than <code>stars</code>.
    ///</summary>
    long? ConvertStars { get; set; }
}
