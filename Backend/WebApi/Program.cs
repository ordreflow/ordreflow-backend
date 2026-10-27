using Application;
using Application.Extenstions;
using Application.Interfaces;
using Domain.Interfaces.IUnitOfWork;
using Domain.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ObjectMapper;
using Persistence;
using Persistence.Identity;
//using ViaPadel.Infrastructure.Repositories;
using WebAPI.Authentication;
using WebAPI.Common;

Env.NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var port = Environment.GetEnvironmentVariable("DB_PORT");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DB_USERNAME");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

    var connectionString =
        $"Host={host};Port={port};Database={database};Username={username};Password={password}";

    options.UseNpgsql(connectionString);
});

// Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

// Authentication (JWT settings come from the JWT_* environment variables)
var jwtOptions = JwtOptions.FromEnvironment();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IAccessTokenGenerator, JwtTokenGenerator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names from the token, so "sub" and "role" are read as-is.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.SigningKey,
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = JwtTokenGenerator.RoleClaimType,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);

builder.Services.AddApplication();

// Application
builder.Services.AddMappings();
// Repositories and unit of work
builder.Services.AddInfrastructure();
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the Authorize button: log in via POST /api/auth/login and paste the accessToken.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken from POST /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

await IdentitySeeder.SeedAdminAsync(
    app.Services,
    Environment.GetEnvironmentVariable("ADMIN_EMAIL"),
    Environment.GetEnvironmentVariable("ADMIN_PASSWORD"),
    Environment.GetEnvironmentVariable("ADMIN_NAME"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

// Lets the test project start the API with WebApplicationFactory<Program>.
public partial class Program;