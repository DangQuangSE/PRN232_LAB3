using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using PRN232.LMSSystem.StudentService.Data;
using PRN232.LMSSystem.StudentService.Interfaces;
using PRN232.LMSSystem.StudentService.Repositories;
using PRN232.LMSSystem.StudentService.Services;
using PRN232.LMSSystem.StudentService.Helpers;
using PRN232.LMSSystem.StudentService.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Asp.Versioning;
using FluentValidation;
using MassTransit;
using PRN232.LMSSystem.StudentService.Consumers;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    // Configure routing
    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
    });

    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    // Configure API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"),
            new QueryStringApiVersionReader("api-version")
        );
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

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

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
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

    // Register gRPC
    builder.Services.AddGrpc();

    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                           ?? builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<StudentDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Register DataShaper for DTO responses
    builder.Services.AddScoped(typeof(IDataShaper<>), typeof(DataShaper<>));

    // Register Repositories
    builder.Services.AddScoped<IStudentRepository, StudentRepository>();

    // Register Services
    builder.Services.AddScoped<IStudentService, StudentService>();

    // Register FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentRequestValidator>();

    // Configure MassTransit with RabbitMQ
    var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<EnrollmentCreatedConsumer>();
        x.AddConsumer<EnrollmentStatusChangedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    });

    // Configure OpenTelemetry Distributed Tracing
    var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "student-service";
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter());

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PRN232 LMS Student Service RESTful API v1",
            Version = "v1",
            Description = "An ASP.NET Core Web API for the Student Microservice (Version 1)."
        });

        // Configure Swagger JWT Authentication Testing
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token below."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseMiddleware<PRN232.LMSSystem.StudentService.Middleware.GlobalExceptionMiddleware>();
    app.UseMiddleware<PRN232.LMSSystem.StudentService.Middleware.LoggingMiddleware>();

    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRN232 LMS Student API v1");
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    
    // Map gRPC service
    app.MapGrpcService<StudentGrpcService>();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "StudentService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
