namespace StreamPacketParser.Internal;

internal sealed class ByteBuffer
{
    private byte[] _storage;
    private int _start;
    private int _end;

    public ByteBuffer(int initialCapacity)
    {
        _storage = new byte[Math.Max(initialCapacity, 16)];
    }

    public int Count => _end - _start;

    public ReadOnlySpan<byte> Span => _storage.AsSpan(_start, Count);

    public void Append(ReadOnlySpan<byte> data, int maximumCapacity)
    {
        if (data.IsEmpty)
        {
            return;
        }

        int required = checked(Count + data.Length);
        if (required > maximumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "这段数据放不进当前受限缓存。");
        }

        EnsureCapacity(required, maximumCapacity);
        data.CopyTo(_storage.AsSpan(_end));
        _end += data.Length;
    }

    public void Discard(int count)
    {
        if (count < 0 || count > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _start += count;
        if (_start == _end)
        {
            Clear();
        }
    }

    public byte[] CopyToArray(int count)
    {
        if (count < 0 || count > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        byte[] copy = new byte[count];
        Span[..count].CopyTo(copy);
        return copy;
    }

    public void Clear()
    {
        _start = 0;
        _end = 0;
    }

    private void EnsureCapacity(int required, int maximumCapacity)
    {
        if (_storage.Length - _end >= required - Count)
        {
            return;
        }

        if (_start > 0)
        {
            Span.CopyTo(_storage);
            _end = Count;
            _start = 0;
            if (_storage.Length - _end >= required - Count)
            {
                return;
            }
        }

        int doubledCapacity = _storage.Length > maximumCapacity / 2
            ? maximumCapacity
            : _storage.Length * 2;
        int nextCapacity = Math.Max(doubledCapacity, required);
        if (nextCapacity < required)
        {
            throw new InvalidOperationException("解析缓存无法扩容到需要的大小。");
        }

        Array.Resize(ref _storage, nextCapacity);
    }
}
