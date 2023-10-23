using System.Text;
using System.Threading.Channels;

namespace PyroCache.Entries;

public class Subscription : IEquatable<Subscription>
{
    public required string ClientId { get; init; } = default!;

    public required string ChannelName { get; init; } = default!;

    public CancellationTokenSource TokenSource { get; set; } = new();


    public bool Equals(Subscription? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ClientId == other.ClientId && ChannelName == other.ChannelName;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Subscription)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, ChannelName);
    }
}

public sealed class ChannelCacheEntry : CacheEntryBase<ChannelCacheEntry>
{
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
    {
        SingleReader = false,
        SingleWriter = false
    });

    private readonly HashSet<Subscription> _subscriptions = new();

    public HashSet<Subscription> Subscriptions => _subscriptions;

    public Subscription AddSubscriber(string subscriberId)
    {
        var subscription = new Subscription
        {
            ChannelName = Key,
            ClientId = subscriberId,
            TokenSource = new()
        };

        _subscriptions.Add(subscription);
        return subscription;
    }

    public bool RemoveSubscriber(string subscriberId)
    {
        if (_subscriptions.TryGetValue(
                new Subscription { ChannelName = Key, ClientId = subscriberId },
                out var actual))
        {
            actual.TokenSource.Cancel();
            _subscriptions.Remove(actual);
            return true;
        }

        return false;
    }

    public ValueTask WriteAsync(byte[] value) => _channel.Writer.WriteAsync(value);

    public IAsyncEnumerable<byte[]> ReadAllAsync() => _channel.Reader.ReadAllAsync();

    public override CacheEntryType EntryType => CacheEntryType.Channel;

    protected override Task SerializeCore(Stream stream)
    {
        var keyLength = Key.Length;
        var buffer = new byte[4 + keyLength * 2];

        BitConverter.GetBytes(keyLength).CopyTo(buffer, 0);
        Encoding.UTF8.GetBytes(Key).CopyTo(buffer, 4);

        return stream.WriteAsync(buffer).AsTask();
    }

    public override object Clone()
        => new ChannelCacheEntry { Key = Key };

    public override async Task<ChannelCacheEntry?> Deserialize(Stream stream)
    {
        var buffer = new byte[4];

        await stream.ReadExactlyAsync(buffer);
        var keyLength = BitConverter.ToInt64(buffer);

        buffer = new byte[keyLength * 2];
        await stream.ReadExactlyAsync(buffer);
        var key = Encoding.UTF8.GetString(buffer);

        return new ChannelCacheEntry { Key = key };
    }
}