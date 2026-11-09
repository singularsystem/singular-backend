using SingularSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SingularSystem.Models.Configurations
{
    public class MunicipalityNeckBookConfigConfiguration : IEntityTypeConfiguration<MunicipalityNeckBookConfig>
    {
        public void Configure(EntityTypeBuilder<MunicipalityNeckBookConfig> builder)
        {
            builder.ToTable("MunicipalityNeckBookConfigs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.State)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.City)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.EncryptedApiKey)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.State, x.City })
                .IsUnique();
        }
    }
}
