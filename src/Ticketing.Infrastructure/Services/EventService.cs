using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Events;
using Ticketing.Application.Events.Dtos;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Application.Common.Exceptions;

namespace Ticketing.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly TicketingDbContext _dbContext;

    public EventService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventResponse> CreateAsync(CreateEventRequest request,CancellationToken cancellationToken = default)
    {
        ValidateEvent(request.StartsAt!.Value);

        ValidatePricingTiers(request.PricingTiers);

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Venue = request.Venue.Trim(),
            StartsAt = request.StartsAt.Value,
            TotalCapacity = request.TotalCapacity,
            TicketsSold = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            PricingTiers = request.PricingTiers
                .Select(tier => new PricingTier
                {
                    Id = Guid.NewGuid(),
                    Name = tier.Name.Trim(),
                    Price = tier.Price
                })
                .ToList()
        };

        _dbContext.Events.Add(eventEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(eventEntity);
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id,CancellationToken cancellationToken = default)
    {
        var eventEntity = await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.PricingTiers)
            .FirstOrDefaultAsync(
                e => e.Id == id,
                cancellationToken);

        return eventEntity is null
            ? null
            : MapToResponse(eventEntity);
    }

    public async Task<IReadOnlyList<EventResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var events = await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.PricingTiers)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(cancellationToken);

        return events
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<EventResponse?> UpdateAsync(Guid id,UpdateEventRequest request,CancellationToken cancellationToken = default)
    {
        ValidateEvent(request.StartsAt!.Value);

        var eventEntity = await _dbContext.Events
            .Include(e => e.PricingTiers)
            .FirstOrDefaultAsync(
                e => e.Id == id,
                cancellationToken);

        if (eventEntity is null)
            return null;

        if (request.TotalCapacity < eventEntity.TicketsSold)
        {
            throw new ValidationException(
                "Total capacity cannot be less than the number of tickets already sold.");
        }

        eventEntity.Name = request.Name.Trim();
        eventEntity.Description = request.Description.Trim();
        eventEntity.Venue = request.Venue.Trim();
        eventEntity.StartsAt = request.StartsAt.Value;
        eventEntity.TotalCapacity = request.TotalCapacity;
        eventEntity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(eventEntity);
    }

    public async Task<bool> DeleteAsync(Guid id,CancellationToken cancellationToken = default)
    {
        var eventEntity = await _dbContext.Events
            .FirstOrDefaultAsync(
                e => e.Id == id,
                cancellationToken);

        if (eventEntity is null)
            return false;

        // Soft delete preserves the event and its purchase history for reporting.
        eventEntity.IsDeleted = true;
        eventEntity.DeletedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void ValidateEvent(DateTimeOffset startsAt)
    {
        if (startsAt <= DateTimeOffset.UtcNow)
        {
            throw new ValidationException(
                "Event start time must be in the future.");
        }
    }

    private static void ValidatePricingTiers(IReadOnlyCollection<PricingTierRequest> pricingTiers)
    {
        if (pricingTiers.Count == 0)
        {
            throw new ValidationException(
                "At least one pricing tier is required.");
        }

        // Tier names are unique within an event, ignoring case.
        var hasDuplicateNames = pricingTiers
             .GroupBy(
                tier => tier.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (hasDuplicateNames)
        {
            throw new ValidationException(
                "Pricing tier names must be unique within an event.");
        }
    }
    private static EventResponse MapToResponse(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Name = eventEntity.Name,
            Description = eventEntity.Description,
            Venue = eventEntity.Venue,
            StartsAt = eventEntity.StartsAt,
            TotalCapacity = eventEntity.TotalCapacity,
            TicketsSold = eventEntity.TicketsSold,
            TicketsAvailable =
                eventEntity.TotalCapacity - eventEntity.TicketsSold,
            CreatedAtUtc = eventEntity.CreatedAtUtc,
            UpdatedAtUtc = eventEntity.UpdatedAtUtc,

            PricingTiers = eventEntity.PricingTiers
                .Select(tier => new PricingTierResponse
                {
                    Id = tier.Id,
                    Name = tier.Name,
                    Price = tier.Price
                })
                .ToList()
        };
    }
}