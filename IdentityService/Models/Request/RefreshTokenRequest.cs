using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.IdentityService.Models.Request;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
