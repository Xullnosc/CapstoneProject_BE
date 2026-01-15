using System.Text;
using BusinessObjects.Models;
using DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Services;
using Services.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddDbContext<FctmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("capstoneDb"))
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency injection
//DAO (DataAccess Layer)
builder.Services.AddScoped<IUserDAO, UserDAO>();
builder.Services.AddScoped<IWhitelistDAO, WhitelistDAO>();

//Repositories (Repositories Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWhitelistRepository, WhitelistRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

//Middleware
// AutoMapper
builder.Services.AddAutoMapper(
    cfg => cfg.AddProfile<MappingProfile>(),
    AppDomain.CurrentDomain.GetAssemblies()
);

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder
    .Services.AddAuthentication(options =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
        };
    });

var app = builder.Build();

// Allow enabling Swagger in non-development environments via config or env var.
var enableSwagger =
    app.Environment.IsDevelopment()
    || string.Equals(
        builder.Configuration["EnableSwagger"],
        "true",
        StringComparison.OrdinalIgnoreCase
    )
    || string.Equals(
        Environment.GetEnvironmentVariable("ENABLE_SWAGGER"),
        "true",
        StringComparison.OrdinalIgnoreCase
    );

// Configure the HTTP request pipeline.
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only enable HTTPS redirection when an HTTPS URL is configured (e.g. container has a certificate).
var configuredUrls =
    builder.Configuration["ASPNETCORE_URLS"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (
    !string.IsNullOrEmpty(configuredUrls)
    && configuredUrls.Contains("https", StringComparison.OrdinalIgnoreCase)
)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
};
app.MapControllers();

// Root endpoint: redirect to Swagger when Swagger is enabled, otherwise return a simple status JSON.
app.MapGet(
    "/",
    () =>
    {
        if (enableSwagger)
        {
            return Results.Redirect("/swagger");
        }
        return Results.Json(
            new
            {
                status = "OK",
                message = "API running. Use /weatherforecast or /swagger when enabled.",
            }
        );
    }
);

// Health endpoint for readiness checks
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).WithName("Health");
app.MapGet(
        "/weatherforecast",
        () =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
