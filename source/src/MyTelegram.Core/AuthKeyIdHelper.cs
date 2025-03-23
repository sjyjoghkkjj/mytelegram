using System.Buffers.Binary;

namespace MyTelegram.Core;

public class AuthKeyIdHelper(IHashHelper hashHelper) : IAuthKeyIdHelper, ISingletonDependency
{
    public long GetAuthKeyId(byte[] authKey)
    {
        var shaHash = hashHelper.Sha1(authKey);
        //var auxHash = BitConverter.ToUInt64(shaHash, 0);

        //return BitConverter.ToInt64(shaHash, 8 + 4);

        return BinaryPrimitives.ReadInt64LittleEndian(shaHash.AsSpan(8 + 4));
    }
}