using System.Text.RegularExpressions;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Pubsub;

public static class PUnsubscribe
{
    /// <summary>
    /// PUNSUBSCRIBE pattern [pattern ...]
    /// </summary>
    [Command(Key = "PUNSUBSCRIBE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var patterns = package.Parameters
                .Select(p => new Regex(p))
                .ToArray();

            var subscribedChannels = _cache
                .Entries<ChannelCacheEntry>(e =>
                    e.Subscriptions.TryGetValue(
                        new Subscription { ChannelName = e.Key, ClientId = session.SessionID },
                        out var actual) &&
                    patterns.Any(p => p.IsMatch(actual.ChannelName)))
                .ToList();

            // Remove new subscriber to each channel:
            foreach (var (_, channel) in subscribedChannels)
            {
                channel.RemoveSubscriber(session.SessionID);
            }
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 1)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            if (parameters.Any(p => p.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}