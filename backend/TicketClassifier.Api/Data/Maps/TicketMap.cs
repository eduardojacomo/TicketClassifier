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

        builder.Property(x => x.Assunto)
            .HasMaxLength(500);

        builder.Property(x => x.Descricao)
            .IsRequired();

        builder.Property(x => x.Categoria)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Other");

        builder.Property(x => x.Prioridade)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Medium");

        builder.Property(x => x.Departamento)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Support");

        builder.Property(x => x.Resumo)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Confianca)
            .IsRequired();

        builder.Property(x => x.Justificativa)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.Sentimento)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("neutral");

        builder.Property(x => x.Tags)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.ProcessadoOk)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.RegistroModificado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DataModificacao);
    }
}
