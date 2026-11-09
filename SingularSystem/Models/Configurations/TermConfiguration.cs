namespace SingularSystem.Models.Configurations
{
    using SingularSystem.Models.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TermConfiguration : IEntityTypeConfiguration<Term>
    {
        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.ToTable("terms");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Key, x.Version }).IsUnique();
            builder.HasIndex(x => new { x.Key, x.IsActive });

            builder.Property(x => x.Key).IsRequired().HasMaxLength(80);
            builder.Property(x => x.Version).IsRequired().HasMaxLength(40);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.ContentFormat).IsRequired().HasMaxLength(20);
            builder.Property(x => x.ContentHash).IsRequired().HasMaxLength(64);
        }
    }

}
