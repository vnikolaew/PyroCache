using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Pubsub;

public static class PublishCommand
{
    /// <summary>
    /// PUBLISH channel message
    /// </summary>
    [Command(Key = "PUBLISH")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var channel = package.Parameters[0].Trim();
            var message = package.Parameters[1].Trim();

            if (!_cache.TryGet<ChannelCacheEntry>(channel, out var channelEntry))
            {
                channelEntry = new ChannelCacheEntry { Key = channel };
                _cache.Set(channel, channelEntry);
            }

            await channelEntry!.WriteAsync(Encoding.UTF8.GetBytes(message));
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 2)
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