namespace PyroCache.Entries;

public class ListCacheEntry : CacheEntryBase<ListCacheEntry>
{
    private readonly LinkedList<byte[]> _list = new();

    public event EventHandler<byte[]> OnItemAdded;

    public event EventHandler<byte[]> OnItemRemoved;

    public int Length => _list.Count;

    public byte[]? ItemAt(int index)
    {
        if (index >= _list.Count) return default;
        if (index < 0) index = _list.Count - index % _list.Count;

        if (index <= _list.Count / 2)
        {
            var currNode = _list.First;
            var currIdx = 0;
            while (currIdx < index && currNode is not null)
            {
                currNode = currNode.Next;
                currIdx++;
            }

            return currNode?.Value;
        }
        else
        {
            var currNode = _list.Last;
            var currIdx = _list.Count - 1;
            while (currIdx > index && currNode is not null)
            {
                currNode = currNode.Previous;
                currIdx--;
            }

            return currNode?.Value;
        }
    }

    public IEnumerable<byte[]> Range(int start,
        int end)
    {
        end = Math.Min(end, _list.Count);
        if (start < 0 || start >= _list.Count || end < 0) yield break;

        var currNode = _list.First;
        var currIdx = 0;
        while (currIdx < start && currNode is not null)
        {
            currNode = currNode.Next;
            currIdx++;
        }

        while (currIdx <= end && currNode is not null)
        {
            yield return currNode.Value;
            currNode = currNode.Next;
            currIdx++;
        }
    }

    public void LeftPush(params byte[][] items)
    {
        foreach (var item in items)
        {
            _list.AddFirst(item);
            OnItemAdded?.Invoke(this, item);
        }
    }

    public void RightPush(params byte[][] items)
    {
        foreach (var item in items)
        {
            _list.AddLast(item);
            OnItemAdded?.Invoke(this, item);
        }
    }

    public bool Set(int index,
        byte[] item)
    {
        if (index < 0 || index >= _list.Count) return false;

        var currNode = _list.First;
        var currIdx = 0;
        while (currIdx < index && currNode is not null)
        {
            currNode = currNode.Next;
            currIdx++;
        }

        if (currNode is null) return false;

        currNode.Value = item;
        return true;
    }

    public IEnumerable<byte[]?> LeftPop(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_list.Count == 0) yield return null;
            else
            {
                var value = _list.First!.Value;
                _list.RemoveFirst();
                OnItemRemoved?.Invoke(this, value);
                yield return value;
            }
        }
    }

    public IEnumerable<byte[]?> RightPop(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_list.Count == 0) yield return null;
            else
            {
                var value = _list.Last!.Value;
                _list.RemoveLast();
                OnItemRemoved?.Invoke(this, value);
                yield return value;
            }
        }
    }

    // LIST_LENGTH 1ST_ITEM_LENGTH 1ST_ITEM 2ND_ITEM_LENGTH 2ND_ITEM ...
    public override async Task Serialize(Stream stream)
    {
        var buffer = new byte[1 + 4 + 4 * _list.Count + _list.Sum(e => e.Length)];
        buffer[0] = (byte) CacheEntryType.List;

        var currIdx = 1;
        var sizeBuffer = BitConverter.GetBytes(_list.Count);
        sizeBuffer.CopyTo(buffer, currIdx);
        currIdx += 4;

        foreach (var entry in _list)
        {
            var entrySizeBuffer = BitConverter.GetBytes(entry.Length);
            
            entrySizeBuffer.CopyTo(buffer, currIdx);
            currIdx += 4;
            
            entry.CopyTo(buffer, currIdx);
            currIdx += entry.Length;
        }

        await stream.WriteAsync(buffer);
    }

    public override async Task<ListCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }
}