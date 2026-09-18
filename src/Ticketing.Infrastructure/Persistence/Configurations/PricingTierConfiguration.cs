using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

public class PricingTierConfiguration
    : IEntityTypeConfiguration<PricingTier>
{
    public void Configure(EntityTypeBuilder<PricingTier> builder)
    {
        builder.ToTable("PricingTiers", table =>
        {
            table.HasCheckConstraint(
                "CK_PricingTiers_Price",
                "[Price] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(p => p.Event)
            .WithMany(e => e.PricingTiers)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.EventId, p.Name })
            .IsUnique();
    }
}