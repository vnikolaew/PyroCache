using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Generic;

public static class RandomKey
{
    /// <summary>
    /// RANDOMKEY
    /// </summary>
    [Command(Key = "RANDOMKEY")]
    public sealed class Command : BasePyroCommand
    {
        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var keys = _cache.Items.Keys.ToArray();
            if (keys.Length == 0)
            {
                await session.SendStringAsync($"{Nil}\n");
            }

            var key = keys.RandomItem();
            await session.SendStringAsync($"{key}\n");
        }
    }

    public sealed class Validator : ICommandValidator<Command>
    {
        public ValueTask<ValidationResult> ValidateAsync(
            string[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters.Length != 0)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Incorrect number of parameters."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}