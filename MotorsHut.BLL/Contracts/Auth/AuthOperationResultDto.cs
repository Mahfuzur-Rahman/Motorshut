namespace MotorsHut.BLL.Contracts.Auth;

public sealed class AuthOperationResultDto
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public string? PasswordResetToken { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
