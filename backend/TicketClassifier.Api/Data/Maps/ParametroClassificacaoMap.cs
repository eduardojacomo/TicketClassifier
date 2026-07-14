using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class ParametroClassificacaoMap : IEntityTypeConfiguration<ParametroClassificacao>
{
    public void Configure(EntityTypeBuilder<ParametroClassificacao> builder)
    {
        builder.ToTable("ParametrosClassificacao");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Tipo);

        builder.Property(x => x.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Termo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Alvo)
            .HasMaxLength(200);

        builder.Property(x => x.Ativo)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
