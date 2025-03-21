// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Smsjobs;

///<summary>
/// See <a href="https://corefork.telegram.org/method/smsjobs.updateSettings" />
///</summary>
internal sealed class UpdateSettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Smsjobs.RequestUpdateSettings, IBool>,
    Smsjobs.IUpdateSettingsHandler
{
    protected override Task<IBool> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Smsjobs.RequestUpdateSettings obj)
    {
        throw new NotImplementedException();
    }
}
