using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PRN232.LMSSystem.IdentityService.Entities;
using PRN232.LMSSystem.IdentityService.Interfaces;
using PRN232.LMSSystem.IdentityService.Exceptions;
using PRN232.LMSSystem.IdentityService.Models.Request;
using PRN232.LMSSystem.IdentityService.Models.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PRN232.LMSSystem.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new BadRequestException("Invalid username or password.");
        }

        // Generate Access Token
        var (accessToken, expiresIn) = GenerateJwtToken(user);

        // Generate Refresh Token
        var refreshToken = GenerateRefreshToken();

        // Save Refresh Token to database
        var newRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            UserId = user.UserId
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _refreshTokenRepository.SaveAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken == null)
        {
            throw new BadRequestException("Invalid refresh token.");
        }

        if (refreshToken.Expires < DateTime.UtcNow)
        {
            throw new BadRequestException("Refresh token has expired.");
        }

        if (refreshToken.Revoked != null)
        {
            throw new BadRequestException("Refresh token has already been revoked.");
        }

        // Revoke the old token
        refreshToken.Revoked = DateTime.UtcNow;
        _refreshTokenRepository.Update(refreshToken);

        // Generate new Access and Refresh tokens
        var user = refreshToken.User;
        var (newAccessToken, expiresIn) = GenerateJwtToken(user);
        var newRefreshTokenString = GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenString,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            UserId = user.UserId
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _refreshTokenRepository.SaveAsync();

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresIn = expiresIn
        };
    }

    private (string Token, int ExpiresIn) GenerateJwtToken(User user)
    {
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                        ?? _configuration["Jwt:Secret"] 
                        ?? "YourSuperSecretKeyGoesHereOfMinimumLengthOf32BytesForHS256Algorithm";
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                        ?? _configuration["Jwt:Issuer"] 
                        ?? "LmsIssuer";
                        
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                          ?? _configuration["Jwt:Audience"] 
                          ?? "LmsAudience";

        var expiryMinutes = 60; // 1 hour
        if (int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? _configuration["Jwt:ExpiryMinutes"], out var parsedExpiry))
        {
            expiryMinutes = parsedExpiry;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiryMinutes * 60);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
