using System.ComponentModel.DataAnnotations;

namespace AuthServer.Application.Auth;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(255)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
