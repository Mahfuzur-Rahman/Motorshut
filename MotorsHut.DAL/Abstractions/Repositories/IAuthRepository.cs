using MotorsHut.DAL.Contracts.Auth;

namespace MotorsHut.DAL.Abstractions.Repositories;

public interface IAuthRepository
{
    Task<AuthOperationResult> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> RegisterAdminAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
