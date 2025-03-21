using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MyTelegram.Schema.Extensions;

public sealed class ArrayPoolBufferWriterWrapper<T>(ArrayBufferWriter<T> writer) : IDisposable
{
    public ArrayBufferWriter<T> Writer => writer;

    public int WrittenCount => writer.WrittenCount;

    public void Dispose()
    {
        writer.Clear();
        ArrayBufferWriterPool<T>.Return(this);
    }
}
public class ArrayBufferWriterPool : ArrayBufferWriterPool<byte> { }

public class ArrayBufferWriterPool<T>
{
    private static readonly ConcurrentQueue<ArrayPoolBufferWriterWrapper<T>> _queue = new();

    public static ArrayPoolBufferWriterWrapper<T> Rent(int initialCapacity = 1024)
    {
        if (_queue.TryDequeue(out var writer))
        {
            return writer;
        }
        return new ArrayPoolBufferWriterWrapper<T>(new ArrayBufferWriter<T>(1024));
    }

    public static void Return(ArrayPoolBufferWriterWrapper<T> writerWrapper)
    {
        writerWrapper.Writer.Clear();
        _queue.Enqueue(writerWrapper);
    }
}

public static class TlObjectExtensions
{
    //private static readonly BytesSerializer BytesSerializer = new();
    //[return: NotNullIfNotNull("obj")]
    //public static byte[]? ToBytes(this IObject? obj)
    //{
    //    if (obj == null)
    //    {
    //        return null;
    //    }

    //    var stream = new MemoryStream();
    //    var bw = new BinaryWriter(stream);
    //    obj.Serialize(bw);

    //    return stream.ToArray();
    //}

    public static long? ToPeerId(this IPeer? peer)
    {
        long? ownerId = null;
        switch (peer)
        {
            case null:
                break;
            case TPeerChannel peerChannel:
                ownerId = peerChannel.ChannelId;
                break;
            case TPeerChat peerChat:
                ownerId = peerChat.ChatId;
                break;
            case TPeerUser peerUser:
                ownerId = peerUser.UserId;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ownerId;
    }

    public static IInputPeer ToInputPeer(this IInputUser inputUser)
    {
        switch (inputUser)
        {
            case TInputUser inputUser1:
                return new TInputPeerUser
                {
                    UserId = inputUser1.UserId,
                    AccessHash = inputUser1.AccessHash
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(inputUser));
        }
    }

    public static IInputPeer ToInputPeer(this IInputChannel inputChannel)
    {
        if (inputChannel is TInputChannel tInputChannel)
        {
            return new TInputPeerChannel
            {
                ChannelId = tInputChannel.ChannelId,
                AccessHash = tInputChannel.AccessHash
            };
        }

        throw new NotSupportedException($"Not supported channel: {inputChannel.GetType().Name}");
    }

    public static long GetReactionId(this IReaction reaction)
    {
        switch (reaction)
        {
            case TReactionCustomEmoji reactionCustomEmoji:
                return reactionCustomEmoji.DocumentId;
            case TReactionEmoji reactionEmoji:
                var bytes = Encoding.UTF8.GetBytes(reactionEmoji.Emoticon).AsSpan();
                if (bytes.Length >= 8)
                {
                    return BinaryPrimitives.ReadInt64LittleEndian(bytes);
                }
                Span<byte> newBytes = stackalloc byte[8];
                bytes.CopyTo(newBytes);

                return BinaryPrimitives.ReadInt64LittleEndian(newBytes);

            case TReactionEmpty:
                return 0;
            //return reactionEmpty.ConstructorId;

            case TReactionPaid reactionPaid:
                return reactionPaid.ConstructorId;

            default:
                throw new ArgumentOutOfRangeException(nameof(reaction));
        }
    }

    public static long GetFileId(this IInputFile? file)
    {
        switch (file)
        {
            case null:
                return 0;
            case TInputFile inputFile:
                return inputFile.Id;
            case TInputFileBig inputFileBig:
                return inputFileBig.Id;
            case TInputFileStoryDocument inputFileStoryDocument:
                switch (inputFileStoryDocument.Id)
                {
                    case TInputDocument inputDocument:
                        return inputDocument.Id;
                    case TInputDocumentEmpty:
                        return 0;
                }
                break;
        }

        return 0;
    }


    [return: NotNullIfNotNull(nameof(inputReplyTo))]
    public static int? ToReplyToMsgId(this IInputReplyTo? inputReplyTo)
    {
        switch (inputReplyTo)
        {
            case TInputReplyToMessage inputReplyToMessage:
                return inputReplyToMessage.ReplyToMsgId;
            case TInputReplyToStory inputReplyToStory:
                return inputReplyToStory.StoryId;
        }

        return null;
    }

    public static int GetLength(this IObject? obj)
    {
        if (obj == null)
        {
            return 0;
        }

        var writer = ArrayBufferWriterPool.Rent();
        int count;
        try
        {
            obj.Serialize(writer.Writer);
            count = writer.WrittenCount;
        }
        finally
        {
            ArrayBufferWriterPool.Return(writer);
        }

        return count;
    }

    [return: NotNullIfNotNull("obj")]
    public static byte[]? ToBytes(this IObject? obj)
    {
        if (obj == null)
        {
            return null;
        }
        var writer = ArrayBufferWriterPool.Rent();

        try
        {
            obj.Serialize(writer.Writer);
            var bytes = writer.Writer.WrittenSpan.ToArray();

            return bytes;
        }
        finally
        {
            ArrayBufferWriterPool.Return(writer);
        }

        //throw new NotImplementedException();
        //if (obj == null)
        //{
        //    return null;
        //}

        //var stream = new MemoryStream();
        //var bw = new BinaryWriter(stream);
        //obj.Serialize(bw);

        //return stream.ToArray();
    }

    //public static ReadOnlyMemory<byte> ToReadonlyMemory(this IObject? obj)
    //{
    //    if (obj == null)
    //    {
    //        return ReadOnlyMemory<byte>.Empty;
    //    }

    //}

    //public static byte[]? ToBytes(this IObject? obj)
    //{
    //    if (obj == null)
    //    {
    //        return null;
    //    }

    //    using var writer = ArrayBufferWriterPool.Rent();
    //    obj.Serialize(writer);
    //    return writer.Writer.WrittenSpan.ToArray();
    //}

    //public static TObject? ToTObject<TObject>(this ReadOnlyMemory<byte> memory) where TObject : IObject
    //{
    //    if (memory.IsEmpty)
    //    {
    //        return default;
    //    }
    //    var serializer = SerializerFactory.CreateObjectSerializer<TObject>();
    //    var buffer = new ReadOnlySequence<byte>(memory);
    //    return serializer.Deserialize(ref buffer);
    //}


    [return: NotNullIfNotNull("readOnlyMemory")]
    public static TObject? ToTObject<TObject>(this ReadOnlyMemory<byte>? readOnlyMemory) where TObject : IObject
    {
        if (readOnlyMemory?.Length > 0)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(readOnlyMemory.Value));
            return reader.Read<TObject>();
        }

        return default;
    }

    public static TObject ToTObject<TObject>(this ReadOnlyMemory<byte> readOnlyMemory) where TObject : IObject
    {
        if (readOnlyMemory.Length > 0)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(readOnlyMemory));
            return reader.Read<TObject>();
        }

        return default;
    }


    [return: NotNullIfNotNull("bytes")]
    public static TObject? ToTObject<TObject>(this byte[]? bytes) where TObject : IObject
    {
        return ToTObject<TObject>(readOnlyMemory: bytes);
    }

    public static TObject ToTObject<TObject>(this Memory<byte> memory) where TObject : IObject
    {
        return ToTObject<TObject>(readOnlyMemory: memory);
    }

    //public static void Serialize<T>(this IBufferWriter<byte> writer,
    //    T value)
    //{
    //    SerializerFactory.CreateSerializer<T>().Serialize(value, writer);
    //}

    //public static T Deserialize<T>(this ref ReadOnlySequence<byte> buffer)
    //{
    //    return SerializerFactory.CreateSerializer<T>().Deserialize(ref buffer);
    //}

    //public static void Serialize<T>(this T value, ArrayPoolBufferWriterWrapper<byte> writerWrapper) where T : IObject
    //{
    //    value.Serialize(writerWrapper.Writer);
    //}

    //public static void Serialize<T>(this ArrayPoolBufferWriterWrapper<byte> writerWrapper,
    //    T value)
    //{
    //    writerWrapper.Writer.Serialize(value);
    //}

    //public static void Serialize<T>(this BinaryWriter writer, T value)
    //{
    //    SerializerFactory.CreateSerializer<T>().Serialize(value, writer);
    //}

    //public static T Deserialize<T>(this BinaryReader reader)
    //{
    //    return SerializerFactory.CreateSerializer<T>().Deserialize(reader);
    //}
}