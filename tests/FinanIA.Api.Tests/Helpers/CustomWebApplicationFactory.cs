using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinanIA.Api.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = $"test_{Guid.NewGuid()}.db";

    public CustomWebApplicationFactory()
    {
        // Environment variables are read during WebApplicationBuilder.CreateBuilder,
        // so they must be set before the factory initializes the host.
        var dbPath = _dbPath;
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={dbPath}");
        Environment.SetEnvironmentVariable("JWT__Secret", "test-secret-key-for-integration-tests-at-least-32-chars!");
        Environment.SetEnvironmentVariable("JWT__Issuer", "test-issuer");
        Environment.SetEnvironmentVariable("JWT__Audience", "test-audience");
        Environment.SetEnvironmentVariable("CORS__AllowedOrigin", "http://localhost:3000");
        Environment.SetEnvironmentVariable("Gemini__ApiKey", "test-gemini-api-key-placeholder");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        // Clean up env vars set for this test run
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        Environment.SetEnvironmentVariable("JWT__Secret", null);
        Environment.SetEnvironmentVariable("JWT__Issuer", null);
        Environment.SetEnvironmentVariable("JWT__Audience", null);
        Environment.SetEnvironmentVariable("CORS__AllowedOrigin", null);
        Environment.SetEnvironmentVariable("Gemini__ApiKey", null);

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
