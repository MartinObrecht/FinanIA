using System.Text;
using FinanIA.Api.Middleware;
using Scalar.AspNetCore;
using FinanIA.Application.Assistant;
using FinanIA.Application.Auth;
using FinanIA.Application.Auth.Commands;
using FinanIA.Application.Transactions.Commands;
using FinanIA.Application.Transactions.Queries;
using FinanIA.Domain.Interfaces;
using FinanIA.Infrastructure.Assistant;
using FinanIA.Infrastructure.Auth;
using FinanIA.Infrastructure.Persistence;
using FinanIA.Infrastructure.Persistence.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Mscc.GenerativeAI.Microsoft;

var builder = WebApplication.CreateBuilder(args);

// EF Core SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration["ConnectionStrings__Default"]
        ?? builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=finania.db"));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// JWT token service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// JWT authentication
var jwtSecret = builder.Configuration["JWT__Secret"] ?? builder.Configuration["JWT:Secret"]
    ?? throw new InvalidOperationException("JWT secret is not configured.");
var jwtIssuer = builder.Configuration["JWT__Issuer"] ?? builder.Configuration["JWT:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["JWT__Audience"] ?? builder.Configuration["JWT:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as-is (e.g. "sub" stays "sub", not mapped to ClaimTypes.NameIdentifier)
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigin = builder.Configuration["CORS__AllowedOrigin"] ?? builder.Configuration["CORS:AllowedOrigin"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrEmpty(allowedOrigin))
            policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandHandler>();

// Command handlers
builder.Services.AddScoped<RegisterUserCommandHandler>();
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();
builder.Services.AddScoped<LogoutCommandHandler>();
builder.Services.AddScoped<CreateTransactionCommandHandler>();
builder.Services.AddScoped<UpdateTransactionCommandHandler>();
builder.Services.AddScoped<DeleteTransactionCommandHandler>();
builder.Services.AddScoped<GetTransactionsQueryHandler>();
builder.Services.AddScoped<GetBalanceQueryHandler>();

// Gemini IChatClient
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? builder.Configuration["Gemini__ApiKey"]
    ?? throw new InvalidOperationException("Gemini API key is not configured. Set 'Gemini:ApiKey' via dotnet user-secrets or 'GEMINI_API_KEY' environment variable.");
builder.Services.AddSingleton<IChatClient>(sp =>
    new GeminiChatClient(apiKey: geminiApiKey, model: "gemini-2.5-flash")
        .AsBuilder()
        .UseFunctionInvocation(configure: client => client.MaximumIterationsPerRequest = 5)
        .Build(sp));

// AI Financial Assistant
builder.Services.AddScoped<IFinancialAssistant, GeminiFinancialAssistant>();
builder.Services.AddScoped<AskAssistantCommandHandler>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// OpenAPI / Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
