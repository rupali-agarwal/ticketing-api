using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.IntegrationTests.Infrastructure;

public class TicketingApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=TicketingIntegrationTestsDb;Trusted_Connection=True;TrustServerCertificate=True";
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Integration");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TicketingDatabase"] =
                        TestConnectionString
                });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        // Use the real SQL Server schema so integration tests exercise the same database behaviour relied on by the application.
        await dbContext.Database.MigrateAsync();

        // Clear dependent entities first to respect foreign key constraints.
        await dbContext.TicketPurchases.ExecuteDeleteAsync();
        await dbContext.PricingTiers.ExecuteDeleteAsync();
        await dbContext.Events.ExecuteDeleteAsync();
    }
}