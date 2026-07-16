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
            .HasColumnName("Assunto")
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasColumnName("Descricao")
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("Categoria")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Other");

        builder.Property(x => x.Priority)
            .HasColumnName("Prioridade")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Medium");

        builder.Property(x => x.Department)
            .HasColumnName("Departamento")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Support");

        builder.Property(x => x.Summary)
            .HasColumnName("Resumo")
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Confidence)
            .HasColumnName("Confianca")
            .IsRequired();

        builder.Property(x => x.Justification)
            .HasColumnName("Justificativa")
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Sentiment)
            .HasColumnName("Sentimento")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("neutral");

        builder.Property(x => x.Tags)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.ProcessedOk)
            .HasColumnName("ProcessadoOk")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.RecordModified)
            .HasColumnName("RegistroModificado")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ModifiedDate)
            .HasColumnName("DataModificacao");
    }
}
