using SuperSocket.Command;

namespace PyroCache.Commands.Common;

public interface ICommandValidator<in TCommand>
    where TCommand : ICommand
{
    ValueTask<ValidationResult> ValidateAsync(
        string[] parameters,
        CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public string? Error { get; set; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => Error is not null;

    private ValidationResult()
    {
    }

    private ValidationResult(string error) => Error = error;

    public static ValidationResult Success()
        => new();

    public static ValidationResult Failure(string error)
        => new(error);
}