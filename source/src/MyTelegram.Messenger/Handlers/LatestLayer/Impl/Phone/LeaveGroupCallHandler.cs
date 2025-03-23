// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Phone;

///<summary>
/// Leave a group call
/// See <a href="https://corefork.telegram.org/method/phone.leaveGroupCall" />
///</summary>
internal sealed class LeaveGroupCallHandler : RpcResultObjectHandler<MyTelegram.Schema.Phone.RequestLeaveGroupCall, MyTelegram.Schema.IUpdates>,
    Phone.ILeaveGroupCallHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Phone.RequestLeaveGroupCall obj)
    {
        throw new NotImplementedException();
    }
}
