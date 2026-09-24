using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common.Exceptions;
using Ticketing.Application.Tickets;
using Ticketing.Application.Tickets.Dtos;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly TicketingDbContext _dbContext;

    public TicketService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AvailabilityResponse?> GetAvailabilityAsync(Guid eventId,CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new AvailabilityResponse
            {
                EventId = e.Id,
                TotalCapacity = e.TotalCapacity,
                TicketsSold = e.TicketsSold,
                TicketsAvailable = e.TotalCapacity - e.TicketsSold
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TicketPurchaseResponse?> PurchaseAsync(Guid eventId,PurchaseTicketsRequest request,CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        var eventInfo = await _dbContext.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new
            {
                e.Id,
                e.StartsAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
            throw new NotFoundException("Event not found.");

        if (eventInfo.StartsAt <= DateTimeOffset.UtcNow)
            throw new ValidationException("Tickets cannot be purchased for an event that has already started.");

        var pricingTier = await _dbContext.PricingTiers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == request.PricingTierId &&
                     p.EventId == eventId,
                cancellationToken);

        if (pricingTier is null)
            throw new NotFoundException("Pricing tier not found.");

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var rowsAffected = await _dbContext.Events
            .Where(e =>
                e.Id == eventId &&
                e.TicketsSold + request.Quantity <= e.TotalCapacity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        e => e.TicketsSold,
                        e => e.TicketsSold + request.Quantity)
                    .SetProperty(
                        e => e.UpdatedAtUtc,
                        now),
                cancellationToken);

        if (rowsAffected == 0)
            throw new ConflictException(
                "There are not enough tickets available for this purchase.");

        var purchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            PricingTierId = pricingTier.Id,
            Quantity = request.Quantity,
            UnitPrice = pricingTier.Price,
            PurchasedAtUtc = now
        };

        _dbContext.TicketPurchases.Add(purchase);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TicketPurchaseResponse
        {
            Id = purchase.Id,
            EventId = purchase.EventId,
            PricingTierId = purchase.PricingTierId,
            Quantity = purchase.Quantity,
            UnitPrice = purchase.UnitPrice,
            TotalPrice = purchase.Quantity * purchase.UnitPrice,
            PurchasedAtUtc = purchase.PurchasedAtUtc
        };
    }
}