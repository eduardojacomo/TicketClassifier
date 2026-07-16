using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class TicketMap : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.BatchId);

        builder.Property(x => x.ExternalId)
            .HasMaxLength(100);

        builder.Property(x => x.Subject)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Other");

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Medium");

        builder.Property(x => x.Department)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Support");

        builder.Property(x => x.Summary)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Confidence)
            .IsRequired();

        builder.Property(x => x.Justification)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Sentiment)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("neutral");

        builder.Property(x => x.Tags)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.ProcessedOk)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.RecordModified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ModifiedDate);
    }
}
