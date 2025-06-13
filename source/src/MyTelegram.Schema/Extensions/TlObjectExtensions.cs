using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace MyTelegram.Schema.Extensions;

//public sealed class ArrayPoolBufferWriterWrapper<T>(ArrayBufferWriter<T> writer) : IDisposable
//{
//    public ArrayBufferWriter<T> Writer => writer;

//    public int WrittenCount => writer.WrittenCount;

//    public void Dispose()
//    {
//        writer.Clear();
//        ArrayBufferWriterPool<T>.Return(this);
//    }
//}
//public class ArrayBufferWriterPool : ArrayBufferWriterPool<byte> { }

//public class ArrayBufferWriterPool<T>
//{
//    private static readonly ConcurrentQueue<ArrayPoolBufferWriterWrapper<T>> Queue = [];

//    public static ArrayPoolBufferWriterWrapper<T> Rent(int initialCapacity = 1024)
//    {
//        if (Queue.TryDequeue(out var writer))
//        {
//            return writer;
//        }
//        return new ArrayPoolBufferWriterWrapper<T>(new ArrayBufferWriter<T>(initialCapacity));
//    }

//    public static void Return(ArrayPoolBufferWriterWrapper<T> writerWrapper)
//    {
//        writerWrapper.Writer.Clear();
//        Queue.Enqueue(writerWrapper);
//    }
//}

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
                throw new ArgumentOutOfRangeException(nameof(peer));
        }

        return ownerId;
    }

    public static IInputPeer ToInputPeer(this IInputUser inputUser)
    {
        return inputUser switch
        {
            TInputUser inputUser1 => new TInputPeerUser
            {
                UserId = inputUser1.UserId,
                AccessHash = inputUser1.AccessHash
            },
            _ => throw new ArgumentOutOfRangeException(nameof(inputUser))
        };
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
        return inputReplyTo switch
        {
            TInputReplyToMessage inputReplyToMessage => inputReplyToMessage.ReplyToMsgId,
            TInputReplyToStory inputReplyToStory => inputReplyToStory.StoryId,
            _ => null
        };
    }

    public static int GetLength(this IObject? obj)
    {
        if (obj == null)
        {
            return 0;
        }

        var writer = new ArrayPoolBufferWriter<byte>();
        obj.Serialize(writer);

        return writer.WrittenCount;
    }

    //public static void ToBytes(this IObject? obj, Memory<byte> memory)
    //{
    //    if (obj == null)
    //    {
    //        return;
    //    }
    //    var writer = ArrayBufferWriterPool.Rent();
    //    try
    //    {
    //        obj.Serialize(writer.Writer);
    //        var bytes = writer.Writer.WrittenSpan;
    //        bytes.CopyTo(memory.Span);
    //    }
    //    finally
    //    {
    //        ArrayBufferWriterPool.Return(writer);
    //    }
    //}

    [return: NotNullIfNotNull(nameof(obj))]
    public static byte[]? ToBytes(this IObject? obj)
    {
        if (obj == null)
        {
            return null;
        }
        using var writer = new ArrayPoolBufferWriter<byte>();

        obj.Serialize(writer);
        var bytes = writer.WrittenSpan.ToArray();

        return bytes;
    }

    [return: NotNullIfNotNull(nameof(readOnlyMemory))]
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


    [return: NotNullIfNotNull(nameof(bytes))]
    public static TObject? ToTObject<TObject>(this byte[]? bytes) where TObject : IObject
    {
        return ToTObject<TObject>(readOnlyMemory: bytes);
    }

    public static TObject ToTObject<TObject>(this Memory<byte> memory) where TObject : IObject
    {
        return ToTObject<TObject>(readOnlyMemory: memory);
    }
}