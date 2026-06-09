using System.Text;
using BreakThroughCV.API.Models;
using BreakThroughCV.API.Services;
using BreakThroughCV.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuthSettings"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));
builder.Services.Configure<PayOsSettings>(builder.Configuration.GetSection("PayOsSettings"));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

// Services
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<GoogleAuthService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHttpClient<PayOsService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PdfTextService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing. Ensure appsettings.json is present and contains a valid JwtSettings section.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminSettings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(adminSettings.BootstrapAdminEmail))
    {
        var db = scope.ServiceProvider.GetRequiredService<MongoDbService>();
        var email = adminSettings.BootstrapAdminEmail.Trim();
        db.Users.UpdateOneAsync(
            u => u.Email == email,
            Builders<User>.Update.Set(u => u.Role, "admin")
        ).GetAwaiter().GetResult();
    }
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
