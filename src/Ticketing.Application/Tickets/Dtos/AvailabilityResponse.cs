
namespace Ticketing.Application.Tickets.Dtos
{
    public class AvailabilityResponse
    {
        public Guid EventId { get; set; }

        public int TotalCapacity { get; set; }

        public int TicketsSold { get; set; }

        public int TicketsAvailable { get; set; }
    }
}
