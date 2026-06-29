using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using PRN232.LMSSystem.CourseService.Data;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Repositories;
using PRN232.LMSSystem.CourseService.Services;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.Grpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Asp.Versioning;
using FluentValidation;
using Serilog;
using Polly;
using Polly.Extensions.Http;
using MassTransit;
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

    // Register gRPC Client for Student Service
    var studentServiceUrl = Environment.GetEnvironmentVariable("STUDENT_SERVICE_URL")
                            ?? builder.Configuration["GrpcClients:StudentService"]
                            ?? "http://localhost:5001";

    // Polly policies for gRPC client resilience
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, _) =>
            {
                Log.Warning("gRPC call to StudentService failed. Retry {Attempt} in {Delay}s. Error: {Error}",
                    attempt, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });

    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                Log.Error("Circuit breaker OPENED for StudentService. Will pause for {Duration}s. Error: {Error}",
                    duration.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            },
            onReset: () => Log.Information("Circuit breaker CLOSED. StudentService is back."),
            onHalfOpen: () => Log.Warning("Circuit breaker HALF-OPEN. Testing StudentService..."));

    builder.Services.AddGrpcClient<StudentGrpc.StudentGrpcClient>(options =>
    {
        options.Address = new Uri(studentServiceUrl);
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

    // Register Redis Cache
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
                   ?? builder.Configuration["Redis:ConnectionString"]
                   ?? "localhost:6379";

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisUrl;
        options.InstanceName = "lms_course:";
    });

    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                           ?? builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<CourseDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Register DataShaper for DTO responses
    builder.Services.AddScoped(typeof(IDataShaper<>), typeof(DataShaper<>));

    // Register Repositories
    builder.Services.AddScoped<ICourseRepository, CourseRepository>();
    builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
    builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
    builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

    // Register Services
    builder.Services.AddScoped<ICourseService, CourseService>();
    builder.Services.AddScoped<ISemesterService, SemesterService>();
    builder.Services.AddScoped<ISubjectService, SubjectService>();
    builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

    // Register FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<SemesterRequestValidator>();

    // Configure MassTransit with RabbitMQ
    var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        });
    });

    // Configure OpenTelemetry Distributed Tracing
    var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "course-service";
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
            Title = "PRN232 LMS Course Service RESTful API v1",
            Version = "v1",
            Description = "An ASP.NET Core Web API for the Course Microservice (Version 1)."
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

    app.UseMiddleware<PRN232.LMSSystem.CourseService.Middleware.GlobalExceptionMiddleware>();
    app.UseMiddleware<PRN232.LMSSystem.CourseService.Middleware.LoggingMiddleware>();

    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRN232 LMS Course API v1");
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CourseService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
