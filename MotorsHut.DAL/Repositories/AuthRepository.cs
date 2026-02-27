using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Contracts.Auth;
using MotorsHut.DAL.Data;
using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private const string CustomerRoleName = "Customer";
    private const string AdminRoleName = "Admin";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly MotorsHutDbContext _context;

    public AuthRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        MotorsHutDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _context = context;
    }

    public Task<AuthOperationResult> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return RegisterInternalAsync(request, CustomerRoleName, cancellationToken);
    }

    public Task<AuthOperationResult> RegisterAdminAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return RegisterInternalAsync(request, AdminRoleName, cancellationToken);
    }

    public async Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Failure("Invalid credentials.");
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return AuthOperationResult.Failure("Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return AuthOperationResult.Success("Login successful.", roles: roles);
    }

    public async Task<AuthOperationResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Success("If the account exists, a reset token has been generated.");
        }

        var rawToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var tokenRecord = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = ComputeSha256(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

        await _context.PasswordResetTokens.AddAsync(tokenRecord, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Reset token generated.", rawToken);
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Failure("Invalid reset request.");
        }

        var tokenHash = ComputeSha256(request.Token);
        var tokenRecord = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.UserId == user.Id && t.UsedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow,
                cancellationToken);

        if (tokenRecord is null)
        {
            return AuthOperationResult.Failure("Invalid or expired reset token.");
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return AuthOperationResult.Failure("Password reset failed.", resetResult.Errors.Select(e => e.Description));
        }

        tokenRecord.UsedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success("Password reset successful.");
    }

    private async Task<AuthOperationResult> RegisterInternalAsync(RegisterRequest request, string roleName, CancellationToken cancellationToken)
    {
        var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail is not null)
        {
            return AuthOperationResult.Failure("Registration failed.", "Email already exists.");
        }

        var existingUserByUserName = await _userManager.FindByNameAsync(request.UserName);
        if (existingUserByUserName is not null)
        {
            return AuthOperationResult.Failure("Registration failed.", "Username already exists.");
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthOperationResult.Failure("Registration failed.", createResult.Errors.Select(e => e.Description));
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var roleCreateResult = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!roleCreateResult.Succeeded)
            {
                return AuthOperationResult.Failure("Failed to create role.", roleCreateResult.Errors.Select(e => e.Description));
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!addRoleResult.Succeeded)
        {
            return AuthOperationResult.Failure("Failed to assign role.", addRoleResult.Errors.Select(e => e.Description));
        }

        return AuthOperationResult.Success($"{roleName} registration successful.");
    }

    private static string ComputeSha256(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }
}
