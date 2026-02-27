using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Contracts.Auth;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Contracts.Auth;

namespace MotorsHut.BLL.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    public AuthService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<AuthOperationResultDto> RegisterUserAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var dalResult = await _authRepository.RegisterUserAsync(new RegisterRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);

        return Map(dalResult);
    }

    public async Task<AuthOperationResultDto> RegisterAdminAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var dalResult = await _authRepository.RegisterAdminAsync(new RegisterRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);

        return Map(dalResult);
    }

    public async Task<AuthOperationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var dalResult = await _authRepository.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);

        return Map(dalResult);
    }

    public async Task<AuthOperationResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var dalResult = await _authRepository.ForgotPasswordAsync(new ForgotPasswordRequest
        {
            Email = request.Email
        }, cancellationToken);

        return Map(dalResult);
    }

    public async Task<AuthOperationResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var dalResult = await _authRepository.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = request.Email,
            Token = request.Token,
            NewPassword = request.NewPassword
        }, cancellationToken);

        return Map(dalResult);
    }

    private static AuthOperationResultDto Map(AuthOperationResult dalResult)
    {
        return new AuthOperationResultDto
        {
            Succeeded = dalResult.Succeeded,
            Message = dalResult.Message,
            Errors = dalResult.Errors,
            PasswordResetToken = dalResult.PasswordResetToken,
            Roles = dalResult.Roles
        };
    }
}
