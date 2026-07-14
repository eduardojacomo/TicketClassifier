using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class TicketSimilaridadeMap : IEntityTypeConfiguration<TicketSimilaridade>
{
    public void Configure(EntityTypeBuilder<TicketSimilaridade> builder)
    {
        builder.ToTable("Similaridades");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TicketOrigemId);
        builder.HasIndex(x => x.TicketRelacionadoId);

        builder.Property(x => x.TicketOrigemId)
            .IsRequired();

        builder.Property(x => x.TicketRelacionadoId)
            .IsRequired();

        builder.Property(x => x.TagsCompartilhadas)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.DataCriacao)
            .IsRequired();
    }
}
