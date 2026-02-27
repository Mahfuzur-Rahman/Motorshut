using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Contracts.Auth;
using MotorsHut.DAL.Entities;
using MotorsHut.Models.Admin;
using MotorsHut.Models.Auth;

namespace MotorsHut.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet("/login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (isAdmin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthOperationResultDto result;
        try
        {
            result = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            }, cancellationToken);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Database is not connected yet. Please connect Aiven MySQL and try again.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            return View(model);
        }

        TempData["SuccessMessage"] = "Login successful.";
        var isAdmin = result.Roles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
        return isAdmin
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpGet("/change-password")]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction("Dashboard", "Admin");
    }

    [AllowAnonymous]
    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            Email = model.Email
        }, cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            return View(model);
        }

        model.GeneratedToken = result.PasswordResetToken;
        model.ResetLink = string.IsNullOrWhiteSpace(result.PasswordResetToken)
            ? null
            : Url.Action("ResetPassword", "Account", new { email = model.Email, token = result.PasswordResetToken }, Request.Scheme);
        ViewData["SuccessMessage"] = "If your account exists, a password reset request has been created.";
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/reset-password")]
    public IActionResult ResetPassword(string? email, string? token)
    {
        return View(new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Token = token ?? string.Empty
        });
    }

    [AllowAnonymous]
    [HttpPost("/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Email = model.Email,
            Token = model.Token,
            NewPassword = model.NewPassword
        }, cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Count > 0 ? string.Join(" ", result.Errors) : result.Message;
            ModelState.AddModelError(string.Empty, errors);
            return View(model);
        }

        TempData["SuccessMessage"] = "Password reset successful. Please login with your new password.";
        return RedirectToAction("Login");
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "You have been logged out.";
        return RedirectToAction("Login");
    }
}
