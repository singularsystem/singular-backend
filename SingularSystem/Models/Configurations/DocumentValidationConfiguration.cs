using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SingularSystem.Models.Entities;

namespace SingularSystem.Models.Configurations;

public class DocumentValidationConfiguration : IEntityTypeConfiguration<DocumentValidation>
{
    public void Configure(EntityTypeBuilder<DocumentValidation> builder)
    {
        builder.ToTable("DocumentValidations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TokenProtected)
            .IsRequired();

        builder.Property(x => x.IssuedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.DocumentId)
            .IsUnique();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();
    }
}
