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
using ObjectMapper;
using Persistence;
using Persistence.Identity;
using System.Security.Claims;
//using ViaPadel.Infrastructure.Repositories;
using WebAPI.Authentication;
using WebAPI.Common;

Env.Load();

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
builder.Services.AddAuthorization();

builder.Services.AddApplication();

// Application
builder.Services.AddMappings();
// Repositories and unit of work
builder.Services.AddInfrastructure();
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await IdentitySeeder.SeedAdminAsync(
    app.Services,
    Environment.GetEnvironmentVariable("ADMIN_EMAIL"),
    Environment.GetEnvironmentVariable("ADMIN_PASSWORD"),
    Environment.GetEnvironmentVariable("ADMIN_NAME"));

app.UseAuthentication();

// Development-only identity for Swagger black-box testing.
// Production still requires the real authentication pipeline.
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated != true &&
            Guid.TryParse(Environment.GetEnvironmentVariable("DEV_USER_ID"), out var devUserId))
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, devUserId.ToString()) },
                "Development");

            context.User = new ClaimsPrincipal(identity);
        }

        await next();
    });
}

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