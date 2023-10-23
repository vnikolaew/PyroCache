using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Sets;

public static class SetSMIsMember
{
    
    /// <summary>
    /// SMISMEMBER key member [member ...]
    /// </summary>
    [Command(Key = "SMISMEMBER")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var setKey = package.Parameters[0].Trim();
            var setMembers = package.Parameters[1..].ToArray();
            
            _cache.TryGet<ICacheEntry>(setKey, out var cacheEntry);

            if (cacheEntry is not SetCacheEntry setCacheEntry)
            {
                await session.SendStringAsync($"{Zero}\n");
                return;
            }

            var areMembers = setMembers
                .Select(m => setCacheEntry.IsMember(m))
                .ToList();

            var response = string.Join("\n",
                areMembers.Select((isMember, i) => $"{i + 1}) {(isMember ? 1 : 0)}"));
            
            setCacheEntry.LastAccessedAt = DateTimeOffset.Now;
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
            if (parameters.Length < 2)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var stringKey = parameters[0].Trim();
            if (stringKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}