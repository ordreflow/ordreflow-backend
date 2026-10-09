using Application;
using Application.Extenstions;
using DotNetEnv;
using ObjectMapper;
using Persistence;
using System.Security.Claims;
using WebAPI.Common;

Env.Load();

var builder = WebApplication.CreateBuilder(args);


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

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();