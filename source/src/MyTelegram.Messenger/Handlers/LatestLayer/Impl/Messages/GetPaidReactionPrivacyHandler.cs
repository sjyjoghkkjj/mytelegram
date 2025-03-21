// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// See <a href="https://corefork.telegram.org/method/messages.getPaidReactionPrivacy" />
///</summary>
internal sealed class GetPaidReactionPrivacyHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetPaidReactionPrivacy, MyTelegram.Schema.IUpdates>,
    Messages.IGetPaidReactionPrivacyHandler
{
    protected override Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetPaidReactionPrivacy obj)
    {
        var update = new TUpdatePaidReactionPrivacy
        {
            Private = false
        };

        return Task.FromResult<MyTelegram.Schema.IUpdates>(new TUpdates
        {
            Updates = [update],
            Chats = [],
            Date = CurrentDate,
            Users = []
        });
    }
}
