using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", table =>
        {
            table.HasCheckConstraint(
                "CK_Events_TotalCapacity",
                "[TotalCapacity] > 0");

            table.HasCheckConstraint(
                "CK_Events_TicketsSold",
                "[TicketsSold] >= 0 AND [TicketsSold] <= [TotalCapacity]");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Venue)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.StartsAt)
            .IsRequired();

        builder.Property(e => e.TotalCapacity)
            .IsRequired();

        builder.Property(e => e.TicketsSold)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .IsRequired();

        // Deleted events remain in the database but are excluded from normal queries.
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}