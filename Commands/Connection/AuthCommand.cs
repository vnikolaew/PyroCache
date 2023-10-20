using PyroCache.Commands.Common;
using PyroCache.Extensions;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.ProtoBase;

namespace PyroCache.Commands.Connection;

public static class Auth
{
    /// <summary>
    /// AUTH [username] password
    /// </summary>
    [Command(Key = "AUTH")]
    public sealed class Command : BasePyroCommand
    {
        public static readonly object IsAuthenticatedKey = new();
        
        private const string SuccessMessage = "Successfully authenticated.";
        
        private const string ErrorMessage = "Invalid credentials.";

        public Command(PyroCache cache) : base(cache)
        {
        }

        protected override async ValueTask ExecuteCoreAsync(
            IAppSession session,
            StringPackageInfo package)
        {
            var username = package.Parameters[0].Trim();
            var password = package.Parameters[1].Trim();


            if (username == "USER" && password == "PASS")
            {
                session[IsAuthenticatedKey] = true;
                await session.SendStringAsync($"{SuccessMessage}\n");
            }
            else
            {
                session[IsAuthenticatedKey] = false;
                await session.SendStringAsync($"{ErrorMessage}\n");
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

            var username = parameters[0].Trim();
            if (username.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Username exceeds maximum limit of 1KB."));
            }

            var password = parameters[1].Trim();
            if (password.Length * 2 > StringKeySizeLimitInBytes)
            {
                return ValueTask.FromResult(ValidationResult.Failure("Password exceeds maximum limit of 1KB."));
            }

            return ValueTask.FromResult(ValidationResult.Success());
        }
    }
}