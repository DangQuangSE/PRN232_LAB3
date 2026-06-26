namespace PRN232.LMSSystem.IdentityService.Entities;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}
