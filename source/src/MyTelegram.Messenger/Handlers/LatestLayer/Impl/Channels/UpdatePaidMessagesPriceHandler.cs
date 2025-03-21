// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.Channels;

///<summary>
/// See <a href="https://corefork.telegram.org/method/channels.updatePaidMessagesPrice" />
///</summary>
internal sealed class UpdatePaidMessagesPriceHandler : RpcResultObjectHandler<MyTelegram.Schema.Channels.RequestUpdatePaidMessagesPrice, MyTelegram.Schema.IUpdates>,
    Channels.IUpdatePaidMessagesPriceHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Channels.RequestUpdatePaidMessagesPrice obj)
    {
        return Task.FromResult<IUpdates>(new TUpdates
        {
            Chats = [],
            Updates = [],
            Users = []
        });
    }
}
