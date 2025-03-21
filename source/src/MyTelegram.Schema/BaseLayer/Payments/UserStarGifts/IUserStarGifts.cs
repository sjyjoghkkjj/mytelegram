// ReSharper disable All

namespace MyTelegram.Schema.Payments;

///<summary>
/// <a href="https://corefork.telegram.org/api/gifts">Gifts</a> displayed on a user's profile.
/// See <a href="https://corefork.telegram.org/constructor/payments.UserStarGifts" />
///</summary>
public interface IUserStarGifts : IObject
{
    ///<summary>
    /// Flags, see <a href="https://corefork.telegram.org/mtproto/TL-combinators#conditional-fields">TL conditional fields</a>
    ///</summary>
    BitArray Flags { get; set; }

    ///<summary>
    /// Total number of gifts displayed on the profile.
    ///</summary>
    int Count { get; set; }

    ///<summary>
    /// The gifts.
    /// See <a href="https://corefork.telegram.org/type/UserStarGift" />
    ///</summary>
    TVector<MyTelegram.Schema.IUserStarGift> Gifts { get; set; }

    ///<summary>
    /// Offset for <a href="https://corefork.telegram.org/api/offsets">pagination</a>.
    ///</summary>
    string? NextOffset { get; set; }

    ///<summary>
    /// Users mentioned in the <code>gifts</code> vector.
    /// See <a href="https://corefork.telegram.org/type/User" />
    ///</summary>
    TVector<MyTelegram.Schema.IUser> Users { get; set; }
}
