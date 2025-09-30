namespace MyTelegram.Messenger.Services.Impl;

public interface IInMemoryFileStorage : ISingletonDependency
{
    Task<bool> SavePartAsync(long fileId, int filePart, int totalParts, ReadOnlyMemory<byte> bytes, bool isBig);
    Task<(bool ok, ReadOnlyMemory<byte> data)> TryAssembleAsync(long fileId, int expectedParts, bool isBig);
    Task<(bool ok, ReadOnlyMemory<byte> slice)> GetSliceAsync(long fileId, int offset, int limit);
}

public class InMemoryFileStorage : IInMemoryFileStorage
{
    private sealed record Part(int Index, byte[] Data);

    private readonly ConcurrentDictionary<(long FileId, bool IsBig), List<Part>> _parts = new();
    private readonly ConcurrentDictionary<long, byte[]> _assembled = new();

    public Task<bool> SavePartAsync(long fileId, int filePart, int totalParts, ReadOnlyMemory<byte> bytes, bool isBig)
    {
        if (filePart < 0 || totalParts <= 0 || filePart >= totalParts) return Task.FromResult(false);
        var key = (fileId, isBig);
        var list = _parts.GetOrAdd(key, _ => new List<Part>(totalParts));
        // ensure capacity
        if (list.Count <= filePart)
        {
            for (var i = list.Count; i <= filePart; i++) list.Add(null!);
        }
        list[filePart] = new Part(filePart, bytes.ToArray());
        return Task.FromResult(true);
    }

    public Task<(bool ok, ReadOnlyMemory<byte> data)> TryAssembleAsync(long fileId, int expectedParts, bool isBig)
    {
        if (_assembled.TryGetValue(fileId, out var ready))
        {
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, ready));
        }
        var key = (fileId, isBig);
        if (!_parts.TryGetValue(key, out var list))
        {
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
        }
        if (list.Count < expectedParts || list.Any(p => p == null))
        {
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
        }
        var total = list.Sum(p => p.Data.Length);
        var buffer = new byte[total];
        var offset = 0;
        foreach (var p in list.OrderBy(p => p.Index))
        {
            Buffer.BlockCopy(p.Data, 0, buffer, offset, p.Data.Length);
            offset += p.Data.Length;
        }
        _assembled[fileId] = buffer;
        _parts.TryRemove(key, out _);
        return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, buffer));
    }

    public Task<(bool ok, ReadOnlyMemory<byte> slice)> GetSliceAsync(long fileId, int offset, int limit)
    {
        if (!_assembled.TryGetValue(fileId, out var data))
        {
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
        }
        if (offset < 0 || limit < 0 || offset >= data.Length)
        {
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
        }
        var end = Math.Min(data.Length, offset + limit);
        var len = end - offset;
        return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, new ReadOnlyMemory<byte>(data, offset, len)));
    }
}

