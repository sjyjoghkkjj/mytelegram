namespace MyTelegram.Messenger.Services.Impl;

public interface IFileStorage : ISingletonDependency
{
    Task<bool> SavePartAsync(long fileId, int filePart, int totalParts, ReadOnlyMemory<byte> bytes, bool isBig);
    Task<(bool ok, ReadOnlyMemory<byte> data)> TryAssembleAsync(long fileId, int expectedParts, bool isBig);
    Task<(bool ok, ReadOnlyMemory<byte> slice)> GetSliceAsync(long fileId, int offset, int limit);
    Task CleanupPartsAsync(long fileId);
}

public class InMemoryFileStorage(IOptionsMonitor<MyTelegramMessengerServerOptions> options, ILogger<InMemoryFileStorage> logger) : IFileStorage
{
    private sealed record Part(int Index, byte[] Data);

    private readonly ConcurrentDictionary<(long FileId, bool IsBig), List<Part>> _parts = new();
    private readonly ConcurrentDictionary<long, byte[]> _assembled = new();
    private readonly ConcurrentDictionary<long, object> _locks = new();

    private string RootPath => string.IsNullOrWhiteSpace(options.CurrentValue.FileStoragePath)
        ? Path.Combine(AppContext.BaseDirectory, "files")
        : options.CurrentValue.FileStoragePath!;

    private bool UseDisk => true;

    public Task<bool> SavePartAsync(long fileId, int filePart, int totalParts, ReadOnlyMemory<byte> bytes, bool isBig)
    {
        if (filePart < 0 || totalParts <= 0 || filePart >= totalParts) return Task.FromResult(false);
        if (UseDisk)
        {
            Directory.CreateDirectory(RootPath);
            var dir = Path.Combine(RootPath, fileId.ToString());
            Directory.CreateDirectory(dir);
            var meta = Path.Combine(dir, "meta.txt");
            if (!File.Exists(meta))
            {
                File.WriteAllText(meta, totalParts.ToString());
            }
            var partPath = Path.Combine(dir, $"{(isBig ? "big" : "small")}_{filePart:D6}.part");
            File.WriteAllBytes(partPath, bytes.ToArray());
            return Task.FromResult(true);
        }
        else
        {
            var key = (fileId, isBig);
            var list = _parts.GetOrAdd(key, _ => new List<Part>(totalParts));
            if (list.Count <= filePart)
            {
                for (var i = list.Count; i <= filePart; i++) list.Add(null!);
            }
            list[filePart] = new Part(filePart, bytes.ToArray());
            return Task.FromResult(true);
        }
    }

    public Task<(bool ok, ReadOnlyMemory<byte> data)> TryAssembleAsync(long fileId, int expectedParts, bool isBig)
    {
        if (UseDisk)
        {
            var dir = Path.Combine(RootPath, fileId.ToString());
            var assembledPath = Path.Combine(dir, "data.bin");
            if (File.Exists(assembledPath))
            {
                var bytes = File.ReadAllBytes(assembledPath);
                return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, bytes));
            }
            var meta = Path.Combine(dir, "meta.txt");
            if (!File.Exists(meta))
            {
                return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
            }
            int totalParts = expectedParts;
            var lockObj = _locks.GetOrAdd(fileId, _ => new object());
            lock (lockObj)
            {
                var partFiles = Directory.GetFiles(dir, "*_*.part").OrderBy(f => f).ToList();
                if (partFiles.Count < totalParts)
                {
                    return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
                }
                using var fs = File.Create(assembledPath);
                foreach (var pf in partFiles)
                {
                    var data = File.ReadAllBytes(pf);
                    fs.Write(data, 0, data.Length);
                }
            }
            var assembled = File.ReadAllBytes(assembledPath);
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, assembled));
        }
        else
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
    }

    public Task<(bool ok, ReadOnlyMemory<byte> slice)> GetSliceAsync(long fileId, int offset, int limit)
    {
        if (UseDisk)
        {
            var dir = Path.Combine(RootPath, fileId.ToString());
            var assembledPath = Path.Combine(dir, "data.bin");
            if (!File.Exists(assembledPath))
            {
                return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
            }
            var fi = new FileInfo(assembledPath);
            var len = (int)Math.Min(Math.Max(0, fi.Length - offset), limit);
            if (len <= 0)
            {
                return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((false, ReadOnlyMemory<byte>.Empty));
            }
            var buffer = new byte[len];
            using var fs = File.OpenRead(assembledPath);
            fs.Seek(offset, SeekOrigin.Begin);
            fs.Read(buffer, 0, len);
            return Task.FromResult<(bool, ReadOnlyMemory<byte>)>((true, buffer));
        }
        else
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

    public Task CleanupPartsAsync(long fileId)
    {
        if (UseDisk)
        {
            try
            {
                var dir = Path.Combine(RootPath, fileId.ToString());
                if (Directory.Exists(dir))
                {
                    foreach (var pf in Directory.GetFiles(dir, "*_*.part"))
                    {
                        File.Delete(pf);
                    }
                    var meta = Path.Combine(dir, "meta.txt");
                    if (File.Exists(meta)) File.Delete(meta);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cleanup parts failed for file {FileId}", fileId);
            }
            return Task.CompletedTask;
        }
        else
        {
            _parts.Keys.Where(k => k.FileId == fileId).ToList().ForEach(k => _parts.TryRemove(k, out _));
            return Task.CompletedTask;
        }
    }
}

