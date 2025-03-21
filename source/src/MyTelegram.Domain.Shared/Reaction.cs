using System.Text;

namespace MyTelegram;

public class Reaction(
    long userId,
    string? emoticon,
    long? customEmojiDocumentId,
    int? date = 0)
{
    public long UserId { get; set; } = userId;
    public string? Emoticon { get; set; } = emoticon;
    public long? CustomEmojiDocumentId { get; set; } = customEmojiDocumentId;

    public int? Date { get; set; } = date;

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