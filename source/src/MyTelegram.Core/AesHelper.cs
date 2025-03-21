using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MyTelegram.Core;

public class AesHelper : IAesHelper, ISingletonDependency
{
    public byte[] EncryptIge(ReadOnlySpan<byte> plainSpan,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv)
    {
        var length = plainSpan.Length;
        var restLength = plainSpan.Length % 16;
        if (restLength % 16 != 0)
        {
            length += 16 - restLength;
        }

        //var plainTextBuffer = new byte[length];
        var plainTextBuffer = GC.AllocateUninitializedArray<byte>(length);

        plainSpan.CopyTo(plainTextBuffer);
        var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        //var aes = new AesManaged { Key = key.ToArray(), Mode = CipherMode.ECB, Padding = PaddingMode.None };
        var blockSize = aes.BlockSize / 8;
        using var encryptor = aes.CreateEncryptor();
        var iv1 = iv[..blockSize];
        var iv2 = iv[blockSize..];
        var cipherTextBlock = new byte[blockSize];
        //var encryptedData = new byte[length];
        var encryptedData = GC.AllocateUninitializedArray<byte>(length);

        //var encryptedBytes = MemoryPool<byte>.Shared.Rent(length);
        Span<byte> encryptedDataSpan = encryptedData;
        for (var i = 0; i < plainTextBuffer.Length; i += blockSize)
        {
            var plainTextBlock = plainTextBuffer[i..(i + blockSize)];
            Xor(plainTextBlock, iv1);
            encryptor.TransformBlock(plainTextBlock,
                0,
                blockSize,
                cipherTextBlock,
                0);
            Xor(cipherTextBlock, iv2);

            iv1 = cipherTextBlock;
            iv2 = plainTextBuffer.AsSpan(i, blockSize); // plainTextBuffer[i..(i + blockSize)];
            //cipherTextBlock.CopyTo(encryptedBytes.Memory.Slice(i, blockSize));
            cipherTextBlock.CopyTo(encryptedDataSpan.Slice(i, blockSize));
        }
        //return encryptedBytes.Memory;

        return encryptedData;
    }

    public void EncryptIge(ReadOnlySpan<byte> source, Span<byte> destination, byte[] key, byte[] iv)
    {
        var outputBytes = ArrayPool<byte>.Shared.Rent(source.Length);
        AesIgeEncryptDecrypt(source, outputBytes, key, iv, true);
        outputBytes.AsSpan(0, source.Length).CopyTo(destination);
        ArrayPool<byte>.Shared.Return(outputBytes);
    }

    public void EncryptIge(ReadOnlySpan<byte> source, byte[] destination, byte[] key, byte[] iv)
    {
        AesIgeEncryptDecrypt(source, destination, key, iv, true);
    }

    public void DecryptIge(ReadOnlySpan<byte> source, Span<byte> destination, byte[] key, byte[] iv)
    {
        var outputBytes = ArrayPool<byte>.Shared.Rent(source.Length);
        AesIgeEncryptDecrypt(source, outputBytes, key, iv, false);
        outputBytes.AsSpan(0, source.Length).CopyTo(destination);
        ArrayPool<byte>.Shared.Return(outputBytes);
    }

    public byte[] DecryptIge(ReadOnlySpan<byte> encryptedSpan,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key.ToArray();

        var blockSize = aes.BlockSize / 8;

        using var decryptor = aes.CreateDecryptor();

        var iv1 = iv[..blockSize];
        var iv2 = iv[blockSize..];
        var plaintextBlock = new byte[blockSize];
        var decryptedData = new byte[encryptedSpan.Length];
        //var decryptedBytes= MemoryPool<byte>.Shared.Rent(encryptedSpan.Length);
        Span<byte> decryptedDataSpan = decryptedData;

        for (var i = 0; i < encryptedSpan.Length; i += blockSize)
        {
            var cipherTextBlock = encryptedSpan[i..(i + blockSize)].ToArray();
            Xor(cipherTextBlock, iv2);

            decryptor.TransformBlock(cipherTextBlock,
                0,
                blockSize,
                plaintextBlock,
                0);

            Xor(plaintextBlock, iv1);
            iv1 = encryptedSpan[i..(i + blockSize)];
            iv2 = plaintextBlock;

            plaintextBlock.CopyTo(decryptedDataSpan[i..]);
            //plaintextBlock.CopyTo(decryptedBytes.Memory[i..]);
        }

        //return decryptedBytes.Memory;
        return decryptedData;
    }

    private void AesIgeEncryptDecrypt(ReadOnlySpan<byte> source, byte[] destination, byte[] key, byte[] iv,
        bool encrypt)
    {
        if (source.Length % 16 != 0)
        {
            throw new ArgumentException("Aes ige input size not divisible by 16");
        }

        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.Zeros;

        //var outputBytes = ArrayPool<byte>.Shared.Rent(source.Length);

        using var cryptor = encrypt ? aes.CreateEncryptor(key, null) : aes.CreateDecryptor(key, null);
        var ivBytes = iv.AsSpan();
        var prevBytes = ivBytes;
        var inputSpan = MemoryMarshal.Cast<byte, long>(source);
        var outputSpan = MemoryMarshal.Cast<byte, long>(destination.AsSpan(0, source.Length));
        var prev = MemoryMarshal.Cast<byte, long>(prevBytes);

        for (int i = 0, count = source.Length / 8; i < count;)
        {
            outputSpan[i] = inputSpan[i] ^ prev[0];
            outputSpan[i + 1] = inputSpan[i + 1] ^ prev[1];
            cryptor.TransformBlock(destination,
                i * 8,
                16,
                destination,
                i * 8);
            prev[0] = outputSpan[i] ^= prev[2];
            prev[1] = outputSpan[i + 1] ^= prev[3];
            prev[2] = inputSpan[i++];
            prev[3] = inputSpan[i++];
        }
    }

    private static void Ctr128Inc(byte[] counter)
    {
        var n = 16;
        var c = 1;
        do
        {
            --n;
            c += counter[n];
            counter[n] = (byte)c;
            c >>= 8;
        } while (n != 0);
    }

    private static void Xor(Span<byte> dest,
        ReadOnlySpan<byte> src)
    {
        for (var i = 0; i < dest.Length; i++)
        {
            dest[i] = (byte)(dest[i] ^ src[i]);
        }
    }
}
