using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Domain.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string Venue { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public int TotalCapacity { get; set; }

        public int TicketsSold { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public ICollection<PricingTier> PricingTiers { get; set; }
            = new List<PricingTier>();
    }
}
