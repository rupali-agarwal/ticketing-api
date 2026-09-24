using Microsoft.Extensions.DependencyInjection;
using Ticketing.Infrastructure.Persistence;
using Ticketing.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Ticketing.IntegrationTests;

public class ApiSmokeTests : IClassFixture<TicketingApiFactory>
{
    private readonly TicketingApiFactory _factory;
    private readonly HttpClient _client;

    public ApiSmokeTests(TicketingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEvents_ReturnsSuccessStatusCode()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.GetAsync("/api/events");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void IntegrationTests_UseIntegrationDatabase()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var connectionString =
            dbContext.Database.GetConnectionString();

        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal(
            "TicketingIntegrationTestsDb",
            builder.InitialCatalog);
    }
}