using Ticketing.Application.Tickets.Dtos;

namespace Ticketing.Application.Tickets;

public interface ITicketService
{
    Task<AvailabilityResponse?> GetAvailabilityAsync(Guid eventId,CancellationToken cancellationToken = default);

    Task<TicketPurchaseResponse> PurchaseAsync(Guid eventId,PurchaseTicketsRequest request,CancellationToken cancellationToken = default);
}