using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Lists;

public static class ListLIndex
{
    /// <summary>
    /// LINDEX key index
    /// </summary>
    [Command(Key = "LINDEX")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var stringKey = package.Parameters[0].Trim();
            var index = int.Parse(package.Parameters[1].Trim());
            
            if (!_cache.TryGet<ListCacheEntry>(stringKey, out var cacheEntry))
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }
            
            if (cacheEntry!.IsExpired)
            {
                // Set item for purging:
                SetItemForPurging(session, cacheEntry);
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var value = cacheEntry!.ItemAt(index);
            if (value is null)
            {
                await session.SendStringAsync($"{Nil}\n");
            }
            else
            {
                cacheEntry.LastAccessedAt = DateTimeOffset.Now;
                await session.SendStringAsync($"{Encoding.UTF8.GetString(value)}\n");
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
            if (parameters.Length != 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            var index = parameters[1].Trim();
            if (!int.TryParse(index, out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Start index should be a whole number."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}