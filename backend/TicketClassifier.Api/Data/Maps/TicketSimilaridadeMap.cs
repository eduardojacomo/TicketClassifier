using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class TicketSimilarityMap : IEntityTypeConfiguration<TicketSimilarity>
{
    public void Configure(EntityTypeBuilder<TicketSimilarity> builder)
    {
        builder.ToTable("Similarities");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.SourceTicketId);
        builder.HasIndex(x => x.RelatedTicketId);

        builder.Property(x => x.SourceTicketId)
            .IsRequired();

        builder.Property(x => x.RelatedTicketId)
            .IsRequired();

        builder.Property(x => x.SharedTags)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.CreatedDate)
            .IsRequired();
    }
}
