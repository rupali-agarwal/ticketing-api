namespace Ticketing.Application.Tickets.Dtos;

public class SalesByTierResponse
{
    public Guid PricingTierId { get; set; }

    public string TierName { get; set; } = string.Empty;

    public int TicketsSold { get; set; }

    public decimal Revenue { get; set; }
}