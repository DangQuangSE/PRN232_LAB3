using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.IdentityService.Models.Request;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
