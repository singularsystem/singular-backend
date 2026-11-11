using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SingularSystem.Models.Entities;

namespace SingularSystem.Models.Configurations;

public class IssuerProfileConfiguration : IEntityTypeConfiguration<IssuerProfile>
{
    public void Configure(EntityTypeBuilder<IssuerProfile> builder)
    {
        builder.ToTable("IssuerProfiles");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ProfessionalRegistration).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.LogoObjectKey).HasMaxLength(500);
        builder.Property(e => e.LogoContentType).HasMaxLength(50);
    }
}
