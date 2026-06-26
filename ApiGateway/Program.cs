using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                ?? builder.Configuration["Jwt:Secret"] 
                ?? "YourSuperSecretKeyGoesHereOfMinimumLengthOf32BytesForHS256Algorithm";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? builder.Configuration["Jwt:Issuer"] 
                ?? "LmsIssuer";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                  ?? builder.Configuration["Jwt:Audience"] 
                  ?? "LmsAudience";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Define an Authorization Policy for the Gateway proxy routes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GatewayAuthPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
