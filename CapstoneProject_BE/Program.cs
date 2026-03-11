using System.Text;
using BusinessObjects.Models;
using DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using Repositories;
using Services;
using Services.Helpers;
using Services.Mappings;
using StackExchange.Redis;
using CapstoneProject_BE.Extensions;

var builder = WebApplication.CreateBuilder(args);

// EPPlus license context (set globally once during startup)
// EPPlus 8+: set license via the static `License` property
ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");

//Redis Configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration =
        builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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
        options.AddPolicy(
            "AllowReactApp",
            builder =>
                builder
                    .WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
        );
    });
}

//Services (Services Layer)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IArchivingService, ArchivingService>();
builder.Services.AddScoped<ICloudinaryHelper, Services.Helpers.CloudinaryHelper>();
builder.Services.AddScoped<ITeamInvitationService, TeamInvitationService>();
builder.Services.AddScoped<IMentorInvitationService, MentorInvitationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IWhitelistService, WhitelistService>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IThesisService, ThesisService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<IThesisFormService, ThesisFormService>();
builder.Services.AddScoped<ILecturerService, LecturerService>();
builder.Services.AddScoped<IAccessLogService, AccessLogService>();

//DAO (DataAccess Layer)
builder.Services.AddScoped<IUserDAO, UserDAO>();
builder.Services.AddScoped<ISystemUserCredentialDAO, SystemUserCredentialDAO>();
builder.Services.AddScoped<IRefreshTokenDAO, RefreshTokenDAO>();
builder.Services.AddScoped<IWhitelistDAO, WhitelistDAO>();
builder.Services.AddScoped<ISemesterDAO, SemesterDAO>();
builder.Services.AddScoped<ITeamDAO, TeamDAO>();
builder.Services.AddScoped<IArchivedWhitelistDAO, ArchivedWhitelistDAO>();
builder.Services.AddScoped<IArchivedTeamDAO, ArchivedTeamDAO>();
builder.Services.AddScoped<ITeamInvitationDAO, TeamInvitationDAO>();
builder.Services.AddScoped<ITeamMemberDAO, TeamMemberDAO>();
builder.Services.AddScoped<IThesisDAO, ThesisDAO>();
builder.Services.AddScoped<IThesisReviewDAO, ThesisReviewDAO>();
builder.Services.AddScoped<IChecklistDAO, ChecklistDAO>();
builder.Services.AddScoped<IThesisFormDAO, ThesisFormDAO>();
builder.Services.AddScoped<ILecturerDAO, LecturerDAO>();
builder.Services.AddScoped<IAccessLogDAO, AccessLogDAO>();
builder.Services.AddScoped<IThesisReviewDAO, ThesisReviewDAO>();

//Repositories (Repositories Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISystemUserCredentialRepository, SystemUserCredentialRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IWhitelistRepository, WhitelistRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IArchivingRepository, ArchivingRepository>();
builder.Services.AddScoped<ITeamInvitationRepository, TeamInvitationRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<IThesisRepository, ThesisRepository>();
builder.Services.AddScoped<IThesisReviewRepository, ThesisReviewRepository>();
builder.Services.AddScoped<IChecklistRepository, ChecklistRepository>();
builder.Services.AddScoped<IThesisFormRepository, ThesisFormRepository>();
builder.Services.AddScoped<ILecturerRepository, LecturerRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddScoped<IThesisReviewRepository, ThesisReviewRepository>();

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

builder.Services.AddAuthorization(options =>
{
    // Reviewer policy: any user with IsReviewer=true (typically lecturers assigned as reviewer)
    options.AddPolicy("Reviewer", policy => policy.RequireClaim("IsReviewer", "true"));

    // Lecturer policy: role claim equals Lecturer
    options.AddPolicy(
        "Lecturer",
        policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim("role", BusinessObjects.CampusConstants.Roles.Lecturer)
            )
    );

    options.AddPolicy(
        "HodOrAdmin",
        policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(BusinessObjects.CampusConstants.Roles.HOD)
                || context.User.IsInRole(BusinessObjects.CampusConstants.Roles.Admin)
            )
    );

    // ReviewerOrHOD: allow either HOD role OR reviewer claim
    options.AddPolicy(
        "ReviewerOrHOD",
        policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(BusinessObjects.CampusConstants.Roles.HOD)
                || context.User.HasClaim("IsReviewer", "true")
            )
    );
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

// Seed default Admin account on first run (if no Admin exists)
app.SeedDefaultAdmin();

// Health endpoint for readiness checks
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).WithName("Health");

app.Run();
