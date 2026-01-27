using System.Text;
using BusinessObjects.Models;
using DataAccess;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories;
using Services;
using Services.Mappings;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddDbContext<FctmsContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("capstoneDb");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Capstone Project API", Version = "v1" });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});
builder.Services.AddHttpClient();

var allowedOrigins = builder.Configuration["AllowedOrigins"];

if (!string.IsNullOrEmpty(allowedOrigins))
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp",
            builder => builder
                .WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .AllowAnyMethod()
                .AllowAnyHeader());
    });
}

// Dependency injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IArchivingService, ArchivingService>();
builder.Services.AddScoped<Services.Helpers.CloudinaryHelper>();

//DAO (DataAccess Layer)
builder.Services.AddScoped<IUserDAO, UserDAO>();
builder.Services.AddScoped<IWhitelistDAO, WhitelistDAO>();
builder.Services.AddScoped<ISemesterDAO, SemesterDAO>();
builder.Services.AddScoped<ITeamDAO, TeamDAO>();
builder.Services.AddScoped<IArchivedWhitelistDAO, ArchivedWhitelistDAO>();
builder.Services.AddScoped<IArchivedTeamDAO, ArchivedTeamDAO>();

//Repositories (Repositories Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWhitelistRepository, WhitelistRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IArchivingRepository, ArchivingRepository>();

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
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

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

app.Run();
