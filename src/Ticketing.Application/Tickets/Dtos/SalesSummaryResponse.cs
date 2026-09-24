namespace Ticketing.Application.Tickets.Dtos;

public class SalesSummaryResponse
{
    public Guid EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int TicketsSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public List<SalesByTierResponse> SalesByTier { get; set; } = [];
}