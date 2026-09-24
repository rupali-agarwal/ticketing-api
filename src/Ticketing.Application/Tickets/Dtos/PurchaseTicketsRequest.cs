using System.ComponentModel.DataAnnotations;

namespace Ticketing.Application.Tickets.Dtos;

public class PurchaseTicketsRequest
{
    [Required]
    public Guid PricingTierId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}