namespace MotorsHut.DAL.Contracts.Auth;

public sealed class AuthOperationResult
{
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();
    public string? PasswordResetToken { get; private init; }
    public IReadOnlyList<string> Roles { get; private init; } = Array.Empty<string>();

    public static AuthOperationResult Success(
        string message,
        string? passwordResetToken = null,
        IEnumerable<string>? roles = null) => new()
    {
        Succeeded = true,
        Message = message,
        PasswordResetToken = passwordResetToken,
        Roles = roles?.ToArray() ?? Array.Empty<string>()
    };

    public static AuthOperationResult Failure(string message, params string[] errors) => new()
    {
        Succeeded = false,
        Message = message,
        Errors = errors
    };

    public static AuthOperationResult Failure(string message, IEnumerable<string> errors) => new()
    {
        Succeeded = false,
        Message = message,
        Errors = errors.ToArray()
    };
}
