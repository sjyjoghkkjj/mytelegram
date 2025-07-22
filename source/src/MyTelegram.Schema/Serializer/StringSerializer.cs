namespace MyTelegram.Schema.Serializer;

public class StringSerializer : ISerializer<string>//, ISerializer2<string>
{
    private readonly BytesSerializer _bytesSerializer = new();


    public void Serialize(string value,
        IBufferWriter<byte> writer)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(value.Length * 4);
        var count = Encoding.UTF8.GetBytes(value, owner.Memory.Span);
        var buffer = owner.Memory.Slice(0, count);
        _bytesSerializer.Serialize(buffer.Span, writer);
    }

    public string Deserialize(ref SequenceReader<byte> reader)
    {
        if (reader.TryRead(out var firstByte))
        {
            var length = 0;
            var padding = 0;

            if (firstByte == 254)
            {
                if (reader.UnreadSequence.Length > 3)
                {
                    length = reader.UnreadSpan[0] | (reader.UnreadSpan[1] << 8) | reader.UnreadSpan[2] << 16;
                    padding = length % 4;
                }
                else
                {
                    throw new ArgumentException("Read buffer length failed");
                }

                reader.Advance(3);
            }
            else
            {
                length = firstByte;
                padding = (length + 1) % 4;
            }

            var sequence = reader.UnreadSequence.Slice(0, length);
            var text = Encoding.UTF8.GetString(sequence);

            reader.Advance(length);

            if (padding > 0)
            {
                padding = 4 - padding;
                reader.Advance(padding);
            }

            return text;
        }

        throw new ArgumentException("Read string from buffer failed");
    }
}