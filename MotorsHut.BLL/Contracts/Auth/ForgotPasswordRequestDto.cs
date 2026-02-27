namespace MotorsHut.BLL.Contracts.Auth;

public sealed class ForgotPasswordRequestDto
{
    public string Email { get; init; } = string.Empty;
}
