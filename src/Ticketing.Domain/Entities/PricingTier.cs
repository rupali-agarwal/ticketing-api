using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Domain.Entities
{
    public class PricingTier
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public required string Name { get; set; }

        public decimal Price { get; set; }

        public Event Event { get; set; } = null!;
    }
}
