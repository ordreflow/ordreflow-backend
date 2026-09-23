using Application;
using Application.Extenstions;
using Domain.Common.IUnitOfWork;
using Domain.Interfaces;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ObjectMapper;
using Persistence;
using ViaPadel.Infrastructure.Repositories;
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
builder.Services.AddApplication();

// Application
builder.Services.AddMappings();
// Repositories
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();