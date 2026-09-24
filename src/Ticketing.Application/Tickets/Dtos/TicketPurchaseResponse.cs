namespace Ticketing.Application.Tickets.Dtos;

public class TicketPurchaseResponse
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid PricingTierId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTimeOffset PurchasedAtUtc { get; set; }
}