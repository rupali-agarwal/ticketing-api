

namespace Ticketing.Application.Events.Dtos
{
    public class PricingTierResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
    }
}
