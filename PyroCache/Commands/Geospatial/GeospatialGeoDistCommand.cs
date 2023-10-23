using System.Text;
using Microsoft.Extensions.Primitives;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Geospatial;

public static class GeospatialGeoDist
{
    /// <summary>
    /// GEODIST key member1 member2 [M | KM | FT | MI]
    /// </summary>
    [Command(Key = "GEODIST")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var indexKey = package.Parameters[0].Trim();
            var memberOneKey = package.Parameters[1].Trim();
            var memberTwoKey = package.Parameters[2].Trim();

            _cache.TryGet<ICacheEntry>(indexKey, out var entry);
            if (entry is not GeospatialIndexCacheEntry geospatialIndexCacheEntry)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            geospatialIndexCacheEntry.LastAccessedAt = DateTimeOffset.Now;
            var memberOne = geospatialIndexCacheEntry.Get(memberOneKey);
            var memberTwo = geospatialIndexCacheEntry.Get(memberTwoKey);
            
            if (memberOne is null || memberTwo is null)
            {
                await session.SendStringAsync($"{Nil}\n");
            }

            var distance = geospatialIndexCacheEntry.Dist(memberOne, memberTwo);
            await session.SendStringAsync($"{distance}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        private const int StringKeySizeLimitInBytes = 1024;

        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length < 3)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            var indexKey = parameters[0].Trim();
            if (indexKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Index key exceeds maximum limit of 1KB."));
            }

            var memberOneKey = parameters[1].Trim();
            if (memberOneKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Member One key exceeds maximum limit of 1KB."));
            }

            var memberTwoKey = parameters[2].Trim();
            if (memberTwoKey.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Member Two key exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}