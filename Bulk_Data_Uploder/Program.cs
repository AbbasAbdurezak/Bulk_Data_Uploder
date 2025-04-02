using AspNetCoreRateLimit;
using Bulk_Data_Uploder.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ====== Critical Fixes Start ====== //
// 1. Rate Limiting Configuration FIRST
builder.Services.AddMemoryCache();

// Client Rate Limiting Services
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting(); // Changed in v5.x
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>(); // Fixed syntax
// ====== Critical Fixes End ====== //

// Database Context (Single registration)
builder.Services.AddDbContext<MultiRegionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Bulk_DB_Primary")));

// JWT Authentication
// JWT Authentication
// ====== Auth Services ====== //
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
}); // <-- Add this line



// Add controllers
builder.Services.AddControllers(); // <-- Add this line

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Bulk_DB_Primary")));
builder.Services.AddHangfireServer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BulkDataUploader API", Version = "v1" });
});

// Application Services
builder.Services.AddScoped<FileParserService>();
builder.Services.AddScoped<AIIntegrationService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<AIResponseVersionService>();

// AI HttpClient
builder.Services.AddHttpClient<AIIntegrationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DeepSeek:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BulkDataUploader API V1"));
}

app.UseHttpsRedirection();
app.UseRouting();

// Critical: Rate Limiting Middleware BEFORE Auth
app.UseClientRateLimiting();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.UseHangfireDashboard();

app.Run();