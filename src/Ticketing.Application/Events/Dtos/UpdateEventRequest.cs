using System.ComponentModel.DataAnnotations;

namespace Ticketing.Application.Events.Dtos
{
    public class UpdateEventRequest
    {
        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(2000)]
        public required string Description { get; set; }

        [Required]
        [MaxLength(300)]
        public required string Venue { get; set; }

        [Required]
        public DateTimeOffset? StartsAt { get; set; }

        [Range(1, int.MaxValue)]
        public int TotalCapacity { get; set; }
    }
}
