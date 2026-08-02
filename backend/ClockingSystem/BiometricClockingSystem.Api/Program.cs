using BiometricClockingSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BiometricClockingSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<BiometricClockingSystem.Api.Services.IFacialRecognitionService,
    BiometricClockingSystem.Api.Services.FacialRecognitionService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.Configure<BiometricClockingSystem.Api.Services.TwilioOptions>(
    builder.Configuration.GetSection(BiometricClockingSystem.Api.Services.TwilioOptions.SectionName));
builder.Services.AddScoped<BiometricClockingSystem.Api.Services.IOtpService, BiometricClockingSystem.Api.Services.OtpService>();
builder.Services.AddScoped<BiometricClockingSystem.Api.Services.IAttendanceService, BiometricClockingSystem.Api.Services.AttendanceService>();


//face record
builder.Services.AddScoped<IFacialRecognitionService, FacialRecognitionService>();
//builder.Services.AddScoped<IFingerprintMatchingService, FingerprintMatchingService>();

// Controllers
builder.Services.AddControllers()
 .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://biometricregister.netlify.app",
            "https://statuesque-selkie-762565.netlify.app"
            "http://127.0.0.1:5173"
        )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.SwaggerDoc("v1", new()
    {
        Title = "Biometric Clocking API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("ReactPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
