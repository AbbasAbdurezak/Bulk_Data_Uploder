using AspNetCoreRateLimit;
using Bulk_Data_Uploder.Services;
using Hangfire;
using Hangfire.PostgreSql;
using IpRateLimiter.AspNetCore.AltairCA.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NPOI.SS.Formula.Functions;
using Serilog;
using Serilog.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Serilog.Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container
builder.Services.AddControllers();

// Configure MultiRegionDbContext with connection string
builder.Services.AddDbContext<MultiRegionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PrimaryDatabase")));


// EF Core
builder.Services.AddDbContext<MultiRegionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PrimaryDatabase")));

// JWT Authentication
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

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("PrimaryDatabase")));
builder.Services.AddHangfireServer();


builder.Services.AddMemoryCache(); // Add this

// Client Rate Limiting
builder.Services.AddMemoryCache(); // Ensure this is called first
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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

//AI Integration
builder.Services.AddHttpClient<AIIntegrationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DeepSeek:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BulkDataUploader API V1"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseClientRateLimiting();

app.MapControllers();
app.UseHangfireDashboard();

app.Run();