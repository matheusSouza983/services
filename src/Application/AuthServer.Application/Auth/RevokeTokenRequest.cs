using System.ComponentModel.DataAnnotations;

namespace AuthServer.Application.Auth;

public sealed class RevokeTokenRequest
{
    [Required]
    [MinLength(20)]
    [MaxLength(2000)]
    public string RefreshToken { get; set; } = string.Empty;
}
