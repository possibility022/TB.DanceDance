using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Quickstart.Account;

public class RegisterViewModel
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    public string ConfirmPassword { get; set; } = null!;

    public string? ReturnUrl { get; set; }
}
