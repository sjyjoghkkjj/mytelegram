namespace MyTelegram;

public record PhotoItem(
    long Id,
    long AccessHash,
    byte[] FileReference,
    int Date,
    int DcId,
    long Size,
    bool HasStickers = false,
    bool HasVideo = false,
    List<PhotoSize>? Sizes = null,
    List<VideoSize>? VideoSizes = null
);