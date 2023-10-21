using System.Threading.Channels;

namespace PyroCache.Entries;

public sealed class ChannelCacheEntry : CacheEntryBase<ChannelCacheEntry>
{
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
    {
        SingleReader = false,
        SingleWriter = false
    });

    public ValueTask WriteAsync(byte[] value) => _channel.Writer.WriteAsync(value);

    public IAsyncEnumerable<byte[]> ReadAllAsync() => _channel.Reader.ReadAllAsync();
    
    public override Task Serialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }

    public override Task<ChannelCacheEntry?> Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }
}