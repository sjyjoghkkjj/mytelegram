namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Sends a request to start a secret chat to the user.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 DH_G_A_INVALID g_a invalid.
/// 400 INPUT_USER_DEACTIVATED The specified user was deleted.
/// 400 USER_ID_INVALID The provided user ID is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.requestEncryption" />
///</summary>
internal sealed class RequestEncryptionHandler(ISecretChatService secretChats, IAccessHashHelper accessHashHelper) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestRequestEncryption, MyTelegram.Schema.IEncryptedChat>
{
    protected override async Task<MyTelegram.Schema.IEncryptedChat> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestRequestEncryption obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.UserId);
        var gA = obj.GA.ToArray();
        var state = secretChats.CreateRequest(input.UserId, (obj.UserId as TInputUser)!.UserId, gA);
        return new TEncryptedChatRequested
        {
            Id = state.ChatId,
            AccessHash = state.AccessHash,
            Date = state.Date,
            AdminId = state.AdminId,
            ParticipantId = state.ParticipantId,
            GA = gA
        };
    }
}
