using System.ComponentModel.DataAnnotations;

namespace MotorsHut.Models.Auth;

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? GeneratedToken { get; set; }
    public string? ResetLink { get; set; }
}
