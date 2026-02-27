using MotorsHut.BLL.Contracts.Auth;

namespace MotorsHut.BLL.Abstractions.Services;

public interface IAuthService
{
    Task<AuthOperationResultDto> RegisterUserAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthOperationResultDto> RegisterAdminAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthOperationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthOperationResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthOperationResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
