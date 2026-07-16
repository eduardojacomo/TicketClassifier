using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class TicketBatchMap : IEntityTypeConfiguration<TicketBatch>
{
    public void Configure(EntityTypeBuilder<TicketBatch> builder)
    {
        builder.ToTable("Batches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasColumnName("NomeArquivo")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Total)
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .HasColumnName("DataCriacao")
            .IsRequired();

        builder.HasMany(x => x.Tickets)
            .WithOne()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
