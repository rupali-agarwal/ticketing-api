using System.Net;
using System.Net.Http.Json;
using Ticketing.Application.Events.Dtos;
using Ticketing.IntegrationTests.Infrastructure;

namespace Ticketing.IntegrationTests;

public class ErrorHandlingTests
    : IClassFixture<TicketingApiFactory>
{
    private readonly TicketingApiFactory _factory;
    private readonly HttpClient _client;

    public ErrorHandlingTests(TicketingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEvent_WhenStartDateIsInPast_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        var request = new
        {
            name = "Past Concert",
            description = "Test event",
            venue = "Test Venue",
            startsAt = DateTimeOffset.UtcNow.AddDays(-1),
            totalCapacity = 100,
            pricingTiers = new[]
            {
                new
                {
                    name = "General",
                    price = 50m
                }
            }
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/events",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task PurchaseTickets_WhenEventDoesNotExist_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();

        var eventId = Guid.NewGuid();

        var request = new
        {
            pricingTierId = Guid.NewGuid(),
            quantity = 1
        };

        var response =
            await _client.PostAsJsonAsync(
                $"/api/events/{eventId}/tickets",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task PurchaseTickets_WhenInsufficientInventory_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        var createEventRequest = new
        {
            name = "Small Concert",
            description = "Test event",
            venue = "Test Venue",
            startsAt = DateTimeOffset.UtcNow.AddDays(10),
            totalCapacity = 2,
            pricingTiers = new[]
            {
            new
            {
                name = "General",
                price = 50m
            }
        }
        };

        var createResponse =
            await _client.PostAsJsonAsync(
                "/api/events",
                createEventRequest);

        createResponse.EnsureSuccessStatusCode();

        var createdEvent =
            await createResponse.Content
                .ReadFromJsonAsync<EventResponse>();

        Assert.NotNull(createdEvent);

        var pricingTierId =
            createdEvent.PricingTiers.Single().Id;

        var purchaseRequest = new
        {
            pricingTierId,
            quantity = 3
        };

        var purchaseResponse =
            await _client.PostAsJsonAsync(
                $"/api/events/{createdEvent.Id}/tickets",
                purchaseRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            purchaseResponse.StatusCode);
    }
}