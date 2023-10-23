using System.Globalization;
using System.Text;
using PyroCache.Commands.Common;
using PyroCache.Entries;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Lists;

public static class ListBrPop
{
    /// <summary>
    /// BRPOP key [key ...] timeout
    /// </summary>
    [Command(Key = "BRPOP")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var listKeys = package.Parameters[..^1].ToArray();
            var timeout = int.Parse(package.Parameters[^1]);

            var listEntries = listKeys
                .Select(key => _cache.TryGet<ListCacheEntry>(key, out var listEntry) ? listEntry : null)
                .Where(_ => _ is not null)
                .ToList();
            if (listEntries.Count == 0)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            // Try and find the first non-empty list:
            var nonEmptyList = listEntries.FirstOrDefault(e => e?.Length > 0);
            if (nonEmptyList is not null)
            {
                await session.SendStringAsync($"1) {nonEmptyList.Key}\n");
                var value = Encoding.UTF8.GetString(nonEmptyList.ItemAt(-1)!);
                await session.SendStringAsync($"2) {value}\n");
                return;
            }

            var tcs = new TaskCompletionSource<(string, byte[]?)?>();
            foreach (var listCacheEntry in listEntries)
            {
                EventHandler<byte[]> onItemAdded = (s, item) =>
                {
                    // Perform LeftPop:
                    _ = listCacheEntry!.RightPop(1);

                    tcs.SetResult((listCacheEntry.Key, item));
                };
                listCacheEntry!.OnItemAdded += onItemAdded;

                // Cleanup event handler:
                await tcs.Task.ContinueWith(_ =>
                    listCacheEntry.OnItemAdded -= onItemAdded);
            }

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            await tcs.Task.WaitAsync(cts.Token);

            if (tcs.Task.Result is null)
            {
                await session.SendStringAsync($"{Nil}\n");
                return;
            }

            var (key, item) = tcs.Task.Result.Value;
            listEntries.FirstOrDefault(e => e.Key == key)!.LastAccessedAt = DateTimeOffset.Now;

            await session.SendStringAsync($"1) {key}\n");
            var itemString = Encoding.UTF8.GetString(item);
            await session.SendStringAsync($"2) {itemString}\n");
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

            if (parameters[..^1].Any(key => key.Length * 2 > StringKeySizeLimitInBytes))
            {
                return ValueTask.FromResult(ValidationResult.Failure("String key exceeds maximum limit of 1KB."));
            }

            var timeout = parameters[^1];
            if (!int.TryParse(timeout, NumberStyles.Integer, new NumberFormatInfo(), out _))
            {
                return ValueTask.FromResult(ValidationResult.Failure("Timeout parameter must be an integer."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}