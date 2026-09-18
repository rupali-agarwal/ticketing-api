using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

public class TicketPurchaseConfiguration
    : IEntityTypeConfiguration<TicketPurchase>
{
    public void Configure(EntityTypeBuilder<TicketPurchase> builder)
    {
        builder.ToTable("TicketPurchases", table =>
        {
            table.HasCheckConstraint(
                "CK_TicketPurchases_Quantity",
                "[Quantity] > 0");

            table.HasCheckConstraint(
                "CK_TicketPurchases_UnitPrice",
                "[UnitPrice] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.PurchasedAtUtc)
            .IsRequired();

       builder.HasOne(p => p.Event)
            .WithMany()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PricingTier)
            .WithMany()
            .HasForeignKey(p => p.PricingTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new
        {
            p.EventId,
            p.PurchasedAtUtc
        });
    }
}