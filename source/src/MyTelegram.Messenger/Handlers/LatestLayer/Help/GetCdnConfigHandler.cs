namespace MyTelegram.Messenger.Handlers.LatestLayer.Help;

///<summary>
/// Get configuration for <a href="https://corefork.telegram.org/cdn">CDN</a> file downloads.
/// See <a href="https://corefork.telegram.org/method/help.getCdnConfig" />
///</summary>
internal sealed class GetCdnConfigHandler(IOptions<MyTelegramMessengerServerOptions> options) : RpcResultObjectHandler<MyTelegram.Schema.Help.RequestGetCdnConfig, MyTelegram.Schema.ICdnConfig>
{
    protected override Task<MyTelegram.Schema.ICdnConfig> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Help.RequestGetCdnConfig obj)
    {
        // Provide CDN public keys for CDN DCs from config (dev keys)
        var keys = new List<ICdnPublicKey>();
        foreach (var dc in options.Value.DcOptions ?? [])
        {
            if (dc.Cdn)
            {
                // For demo, reuse a static RSA public key placeholder; in prod, load from secure store
                keys.Add(new TCdnPublicKey { DcId = dc.Id, PublicKey = "-----BEGIN RSA PUBLIC KEY-----\nMIIBCgKCAQEAuXh...devkey...IDAQAB\n-----END RSA PUBLIC KEY-----" });
            }
        }
        return Task.FromResult<MyTelegram.Schema.ICdnConfig>(new TCdnConfig
        {
            PublicKeys = new TVector<ICdnPublicKey>(keys)
        });
    }
}
