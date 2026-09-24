using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common.Exceptions;
using Ticketing.Application.Events.Dtos;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Services;

namespace Ticketing.UnitTests.Services;

public class EventServiceTests
{
    private static TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TicketingDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WhenStartDateIsInPast_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();

        var service = new EventService(dbContext);

        var request = new CreateEventRequest
        {
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
            TotalCapacity = 100,
            PricingTiers =
            [
                new()
            {
                Name = "General",
                Price = 50m
            }
            ]
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WhenNoPricingTiers_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();

        var service = new EventService(dbContext);

        var request = new CreateEventRequest
        {
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            PricingTiers = []
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WhenPricingTierNamesAreDuplicated_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();

        var service = new EventService(dbContext);

        var request = new CreateEventRequest
        {
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            PricingTiers =
            [
                new()
            {
                Name = "VIP",
                Price = 100m
            },
            new()
            {
                Name = "VIP",
                Price = 150m
            }
            ]
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesEvent()
    {
        await using var dbContext = CreateDbContext();

        var service = new EventService(dbContext);

        var request = new CreateEventRequest
        {
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            PricingTiers =
            [
                new()
            {
                Name = "General",
                Price = 50m
            },
            new()
            {
                Name = "VIP",
                Price = 100m
            }
            ]
        };

        var result = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Event", result.Name);
        Assert.Equal(100, result.TotalCapacity);
        Assert.Equal(0, result.TicketsSold);
        Assert.Equal(2, result.PricingTiers.Count);

        var savedEvent = await dbContext.Events
            .Include(e => e.PricingTiers)
            .SingleAsync();

        Assert.Equal(result.Id, savedEvent.Id);
        Assert.Equal(2, savedEvent.PricingTiers.Count);
    }

    [Fact]
    public async Task UpdateAsync_WhenCapacityIsBelowTicketsSold_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();

        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            TicketsSold = 20,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);

        var request = new UpdateEventRequest
        {
            Name = "Updated Event",
            Description = "Updated Description",
            Venue = "Updated Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(20),
            TotalCapacity = 19
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAsync(eventId, request));
    }

    [Fact]
    public async Task UpdateAsync_WhenCapacityEqualsTicketsSold_UpdatesEvent()
    {
        await using var dbContext = CreateDbContext();

        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            TicketsSold = 20,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);

        var request = new UpdateEventRequest
        {
            Name = "Updated Event",
            Description = "Updated Description",
            Venue = "Updated Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(20),
            TotalCapacity = 20
        };

        var result = await service.UpdateAsync(eventId, request);

        Assert.NotNull(result);
        Assert.Equal(20, result.TotalCapacity);
        Assert.Equal(20, result.TicketsSold);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventIsDeleted_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Deleted Event",
            Description = "Test Description",
            Venue = "Test Venue",
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            TotalCapacity = 100,
            TicketsSold = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = true,
            DeletedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);

        var result = await service.GetByIdAsync(eventId);

        Assert.Null(result);
    }
}