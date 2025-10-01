namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Confirms receipt of messages in a secret chat by client, cancels push notifications.<br>
/// The method returns a list of <strong>random_id</strong>s of messages for which push notifications were cancelled.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 MAX_QTS_INVALID The specified max_qts is invalid.
/// 500 MSG_WAIT_FAILED A waiting call returned an error.
/// See <a href="https://corefork.telegram.org/method/messages.receivedQueue" />
///</summary>
internal sealed class ReceivedQueueHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestReceivedQueue, TVector<long>>
{
    protected override Task<TVector<long>> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestReceivedQueue obj)
    {
        // Ack QTS for the client; in a full impl we'd compute and update server QTS state.
        if (obj.MaxQts <= 0)
        {
            RpcErrors.RpcErrors400.MaxQtsInvalid.ThrowRpcError();
        }
        commandBus.PublishAsync(new MyTelegram.Domain.Commands.Pts.QtsAckedCommand(new MyTelegram.Domain.Aggregates.Pts.PtsId(input.UserId), input.PermAuthKeyId!.Value, obj.MaxQts)).GetAwaiter().GetResult();
        return Task.FromResult(new TVector<long>(obj.Ids));
    }
}
