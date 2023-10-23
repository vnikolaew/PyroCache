using System.Text.RegularExpressions;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Pubsub;

public static class PubsubChannelsCommand
{
    /// <summary>
    /// PUBSUB CHANNELS [pattern]
    /// </summary>
    [Command(Key = "PUBSUB CHANNELS")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var pattern = new Regex(package.Parameters[1].Trim());

            var channels = _cache.Items
                .Where(e => e.Value is ChannelCacheEntry && pattern.IsMatch(e.Key))
                .Select(e => e.Key)
                .ToList();

            var response = channels
                .Select((c, index) => $"{index + 1}) {c}")
                .Join("\n");

            await session.SendStringAsync($"{response}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 1)
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