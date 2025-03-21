// ReSharper disable All

namespace MyTelegram.Schema;

///<summary>
/// A <a href="https://corefork.telegram.org/api/revenue">channel ad revenue »</a> transaction.
/// See <a href="https://corefork.telegram.org/constructor/BroadcastRevenueTransaction" />
///</summary>
[JsonDerivedType(typeof(TBroadcastRevenueTransactionProceeds), nameof(TBroadcastRevenueTransactionProceeds))]
[JsonDerivedType(typeof(TBroadcastRevenueTransactionWithdrawal), nameof(TBroadcastRevenueTransactionWithdrawal))]
[JsonDerivedType(typeof(TBroadcastRevenueTransactionRefund), nameof(TBroadcastRevenueTransactionRefund))]
public interface IBroadcastRevenueTransaction : IObject
{
    ///<summary>
    /// Amount refunded.
    ///</summary>
    long Amount { get; set; }
}
