using System.Text;
using System.Text.RegularExpressions;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Pubsub;

public static class PubsubNumsub
{
    /// <summary>
    /// PUBSUB NUMSUB [channel [channel ...]]
    /// </summary>
    [Command(Key = "PUBSUB NUMSUB")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var channelKeys = package.Parameters[2..].ToHashSet();
            
            var channels = _cache
                .Entries<ChannelCacheEntry>(e =>
                    channelKeys.Contains(e.Key))
                .Select(_ => _.Value)
                .ToList();
            var response = channels
                .Select(c => $"{c.Key} {c.Subscriptions.Count}")
                .Join(" ");
            
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