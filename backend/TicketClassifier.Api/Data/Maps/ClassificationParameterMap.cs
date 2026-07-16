using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data.Maps;

public class ClassificationParameterMap : IEntityTypeConfiguration<ClassificationParameter>
{
    public void Configure(EntityTypeBuilder<ClassificationParameter> builder)
    {
        builder.ToTable("ClassificationParameters");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Type);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Term)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Target)
            .HasMaxLength(200);

        builder.Property(x => x.Active)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
