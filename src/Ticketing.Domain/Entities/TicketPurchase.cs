using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Domain.Entities
{
    public class TicketPurchase
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public Guid PricingTierId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public DateTimeOffset PurchasedAtUtc { get; set; }

        public Event Event { get; set; } = null!;

        public PricingTier PricingTier { get; set; } = null!;
    }
}
