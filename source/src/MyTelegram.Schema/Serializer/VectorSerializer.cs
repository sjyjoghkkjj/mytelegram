namespace MyTelegram.Schema.Serializer;

public class VectorSerializer<T> : ISerializer<TVector<T>>
{
    public void Serialize(TVector<T> value, IBufferWriter<byte> writer)
    {
        writer.Write(value.Count);
        var serializer = SerializerFactory.CreateSerializer<T>();
        foreach (var item in value)
        {
            serializer.Serialize(item, writer);
        }
    }

    public TVector<T> Deserialize(ref SequenceReader<byte> reader)
    {
        if (reader.TryReadLittleEndian(out int count))
        {
            var serializer = SerializerFactory.CreateSerializer<T>();
            var result = new TVector<T>();
            for (int i = 0; i < count; i++)
            {
                var item = serializer.Deserialize(ref reader);
                result.Add(item);
            }

            return result;
        }

        throw new ArgumentException("Read vector count failed.");
    }
}