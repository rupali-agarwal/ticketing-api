using System.Net;
using System.Net.Http.Json;
using Ticketing.Application.Events.Dtos;
using Ticketing.Application.Tickets.Dtos;
using Ticketing.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.IntegrationTests;

public class TicketPurchaseTests
    : IClassFixture<TicketingApiFactory>
{
    private readonly TicketingApiFactory _factory;
    private readonly HttpClient _client;

    public TicketPurchaseTests(TicketingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PurchaseTickets_WhenCapacityIsAvailable_ReturnsSuccess()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange - create an event
        var createEventRequest = new
        {
            name = "Integration Test Concert",
            description = "Test event",
            venue = "Test Venue",
            startsAt = DateTimeOffset.UtcNow.AddDays(10),
            totalCapacity = 10,
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

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdEvent =
                await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        Assert.NotNull(createdEvent);
        Assert.Single(createdEvent.PricingTiers);

        var pricingTier = createdEvent.PricingTiers[0];

        var purchaseRequest = new
        {
            pricingTierId = pricingTier.Id,
            quantity = 3
        };

        var purchaseResponse =
            await _client.PostAsJsonAsync(
                $"/api/events/{createdEvent.Id}/tickets",
                purchaseRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            purchaseResponse.StatusCode);

        var availabilityResponse =
            await _client.GetAsync(
            $"/api/events/{createdEvent.Id}/availability");

        Assert.Equal(
            HttpStatusCode.OK,
            availabilityResponse.StatusCode);

        var availability =
            await availabilityResponse.Content
                .ReadFromJsonAsync<AvailabilityResponse>();

        Assert.NotNull(availability);

        Assert.Equal(10, availability.TotalCapacity);
        Assert.Equal(3, availability.TicketsSold);
        Assert.Equal(7, availability.TicketsAvailable);
    }

    [Fact]
    public async Task ConcurrentPurchases_DoNotOversellEvent()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var createEventRequest = new
        {
            name = "Concurrency Test Concert",
            description = "Testing concurrent purchases",
            venue = "Test Venue",
            startsAt = DateTimeOffset.UtcNow.AddDays(10),
            totalCapacity = 10,
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

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdEvent =
            await createResponse.Content
                .ReadFromJsonAsync<EventResponse>();

        Assert.NotNull(createdEvent);

        var pricingTierId =
            createdEvent.PricingTiers.Single().Id;

        var purchaseRequest = new
        {
            pricingTierId,
            quantity = 1
        };

        // Act - send 20 purchase requests concurrently
        // Send more concurrent purchase requests than the event has capacity for.
        // The atomic database update should allow exactly 10 reservations and reject the remaining requests without overselling.
        var purchaseTasks = Enumerable
            .Range(0, 20)
            .Select(_ =>
                _client.PostAsJsonAsync(
                    $"/api/events/{createdEvent.Id}/tickets",
                    purchaseRequest))
            .ToList();

        var responses =
            await Task.WhenAll(purchaseTasks);

        // Assert
        var successfulPurchases =
            responses.Count(
                response =>
                    response.StatusCode == HttpStatusCode.OK);

        var rejectedPurchases =
            responses.Count(
                response =>
                    response.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(10, successfulPurchases);
        Assert.Equal(10, rejectedPurchases);

        var availabilityResponse =
            await _client.GetAsync(
            $"/api/events/{createdEvent.Id}/availability");

        Assert.Equal(
            HttpStatusCode.OK,
            availabilityResponse.StatusCode);

        var availability =
            await availabilityResponse.Content
                .ReadFromJsonAsync<AvailabilityResponse>();

        Assert.NotNull(availability);

        Assert.Equal(10, availability.TotalCapacity);
        Assert.Equal(10, availability.TicketsSold);
        Assert.Equal(0, availability.TicketsAvailable);

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        // Verify successful inventory reservations and purchase records were committed consistently within the purchase transaction.
        var purchaseCount =
            await dbContext.TicketPurchases
                .CountAsync(p => p.EventId == createdEvent.Id);

        Assert.Equal(10, purchaseCount);
    }
}