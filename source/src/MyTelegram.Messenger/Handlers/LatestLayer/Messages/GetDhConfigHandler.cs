namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// Returns configuration parameters for Diffie-Hellman key generation. Can also return a random sequence of bytes of required length.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 RANDOM_LENGTH_INVALID Random length invalid.
/// See <a href="https://corefork.telegram.org/method/messages.getDhConfig" />
///</summary>
internal sealed class GetDhConfigHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetDhConfig, MyTelegram.Schema.Messages.IDhConfig>
{
    protected override Task<MyTelegram.Schema.Messages.IDhConfig> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetDhConfig obj)
    {
        if (obj.RandomLength is < 0 or > 1024 * 1024)
        {
            RpcErrors.RpcErrors400.RandomLengthInvalid.ThrowRpcError();
        }

        // Layered DH params. Use core MTProto prime and g.
        var prime = AuthConsts.Dh2048P;
        var g = 3;
        var random = obj.RandomLength > 0 ? RandomNumberGenerator.GetBytes(obj.RandomLength) : Array.Empty<byte>();

        var r = new MyTelegram.Schema.Messages.TDhConfig
        {
            G = g,
            P = prime,
            Random = random
        };
        return Task.FromResult<MyTelegram.Schema.Messages.IDhConfig>(r);
    }
}
