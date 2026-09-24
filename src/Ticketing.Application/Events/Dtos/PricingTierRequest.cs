using System.ComponentModel.DataAnnotations;

namespace Ticketing.Application.Events.Dtos
{
    public class PricingTierRequest
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}
