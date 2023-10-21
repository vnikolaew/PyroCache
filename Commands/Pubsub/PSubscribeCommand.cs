using System.Text;
using System.Text.RegularExpressions;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Pubsub;

public static class PSubscribe
{
    /// <summary>
    /// PSUBSCRIBE pattern [pattern ...]
    /// </summary>
    [Command(Key = "PSUBSCRIBE")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var patterns = package.Parameters.Select(p => new Regex(p)).ToArray();

            var channels = _cache
                .Items
                .Where(e => e.Value is ChannelCacheEntry
                            && patterns.Any(p => p.IsMatch(e.Key)))
                .Select(_ => _.Value)
                .Cast<ChannelCacheEntry>()
                .ToList();

            var mergedMessages = channels.Select(c => c.ReadAllAsync()).Merge();
            await foreach (var message in mergedMessages)
            {
                await session.SendStringAsync($"{Encoding.UTF8.GetString(message)}\n");
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