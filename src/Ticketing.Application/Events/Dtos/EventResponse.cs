using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Application.Events.Dtos
{
    public class EventResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Venue { get; set; }
        public DateTimeOffset StartsAt { get; set; }

        public int TotalCapacity { get; set; }
        public int TicketsSold { get; set; }
        public int TicketsAvailable { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? UpdatedAtUtc { get; set; }

        public List<PricingTierResponse> PricingTiers { get; set; } = [];
    }
}
