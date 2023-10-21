using System.Reflection;
using System.Text;

namespace PyroCache.Entries;

public class StringCacheEntry : CacheEntryBase<StringCacheEntry>
{
    public required string Value { get; set; }

    public override async Task Serialize(Stream stream)
    {
        var keyBuffer = Encoding.UTF8.GetBytes(Key);
        var valueBuffer = Encoding.UTF8.GetBytes(Value);
        
        var buffer = new byte[1 + 8 + keyBuffer.Length + valueBuffer.Length];
        buffer[0] = (byte)CacheEntryType.String;
        var currIdx = 1;

        var keySize = BitConverter.GetBytes(keyBuffer.Length);
        keySize.CopyTo(buffer, currIdx);
        currIdx += 4;
        keyBuffer.CopyTo(buffer, currIdx);
        currIdx += keyBuffer.Length;

        var valueSize = BitConverter.GetBytes(Value.Length);
        valueSize.CopyTo(buffer, currIdx);
        currIdx += 4;
        valueBuffer.CopyTo(buffer, currIdx);

        await stream.WriteAsync(buffer);
    }

    public override async Task<StringCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override StringCacheEntry Clone()
        => new() { Key = Key, Value = Value, TimeToLive = TimeToLive };
}