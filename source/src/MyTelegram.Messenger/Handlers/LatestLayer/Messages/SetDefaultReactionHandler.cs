namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Change default emoji reaction to use in the quick reaction menu: the value is synced across devices and can be fetched using <a href="https://corefork.telegram.org/method/help.getConfig">help.getConfig, <code>reactions_default</code> field</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 REACTION_INVALID The specified reaction is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.setDefaultReaction" />
///</summary>
internal sealed class SetDefaultReactionHandler(ICommandBus commandBus) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSetDefaultReaction, IBool>
{
    protected override async Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSetDefaultReaction obj)
    {
        string value = obj.Emoji is TReactionEmoji e ? e.Emoticon : string.Empty;
        var key = nameof(UserConfigType.ReactionsDefault);
        var cmd = new UpdateUserConfigCommand(UserConfigId.Create(input.UserId, key), input.ToRequestInfo(), input.UserId, key, value);
        await commandBus.PublishAsync(cmd);
        return new TBoolTrue();
    }
}
