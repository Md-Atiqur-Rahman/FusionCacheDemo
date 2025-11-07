// File: API/Program.cs
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FusionCache Demo API",
        Version = "v1",
        Description = "Advanced caching with FusionCache and MongoDB"
    });
});

// Configure MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// Configure FusionCache with Redis (L1+L2+Backplane)
// For production multi-server setup
builder.Services.AddFusionCacheWithRedis(builder.Configuration);

// OR use memory-only for single server (uncomment to use)
// builder.Services.AddFusionCacheMemoryOnly(builder.Configuration);

// Register repositories and services
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

var bul= builder.Configuration.GetSection("MongoDb:ConnectionString").Value!;
// Add health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(
        clientFactory: sp => new MongoClient(builder.Configuration.GetSection("MongoDb:ConnectionString").Value!),
        databaseNameFactory: sp => builder.Configuration.GetSection("MongoDb:Database").Value!,
        name: "mongodb",
        tags: new[] { "db", "nosql" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache" });

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();