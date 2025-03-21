using System.Text;

namespace MyTelegram;

public class ReactionCount(
    string? emoticon,
    long? customEmojiDocumentId,
    int count)
{
    public string? Emoticon { get; internal set; } = emoticon;
    public long? CustomEmojiDocumentId { get; internal set; } = customEmojiDocumentId;
    public int Count { get; internal set; } = count;

    public int? ChosenOrder { get; set; }

    public void IncrementCount()
    {
        Count++;
    }

    public void DecrementCount()
    {
        Count--;
    }

    public long GetReactionId()
    {
        if (CustomEmojiDocumentId.HasValue)
        {
            return CustomEmojiDocumentId.Value;
        }

        if (string.IsNullOrEmpty(Emoticon))
        {
            throw new InvalidOperationException("Emotion and CustomEmojiDocumentId is null");
        }
        var bytes = Encoding.UTF8.GetBytes(Emoticon);
        if (bytes.Length >= 8)
        {
            return BitConverter.ToInt64(bytes);
        }

        var newBytes = new byte[8];
        Buffer.BlockCopy(bytes, 0, newBytes, 0, bytes.Length);

        return BitConverter.ToInt64(newBytes);
    }
}