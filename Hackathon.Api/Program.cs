using Hackathon.Application;
using Hackathon.Infrastructure;
using Hackathon.Infrastructure.Persistence;
using Hackathon.Infrastructure.Persistence.Seed;
using Hackathon.Infrastructure.Security;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using Prometheus;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Application / Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks();

// JWT Settings
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = 
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ClockSkew = TimeSpan.Zero
            };
    });

// Authorization
builder.Services.AddAuthorization();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Hackathon API",
            Version = "v1",
            Description = "API do FIAP Hackathon"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "Informe o token JWT usando o formato: Bearer {token}",

            Name = "Authorization",
            In = ParameterLocation.Header,

            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                document)] = []
        });
});

var app = builder.Build();

// Database Seed
var seedDatabase =
    builder.Configuration.GetValue<bool>("SeedDatabase");

if (seedDatabase)
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<HackathonDbContext>();

    await DatabaseSeeder.SeedAsync(dbContext);
}

// Swagger
app.UseSwagger(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Hackathon API v1");

    // Swagger UI na raiz
    options.RoutePrefix = string.Empty;
});

// HTTPS
app.UseHttpsRedirection();

// Authentication / Authorization
app.UseAuthentication();
app.UseAuthorization();

// Prometheus
app.UseHttpMetrics();

// Endpoints
app.MapHealthChecks("/health");
app.MapMetrics();
app.MapControllers();

app.Run();

public partial class Program { }