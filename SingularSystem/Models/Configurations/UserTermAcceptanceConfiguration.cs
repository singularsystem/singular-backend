namespace SingularSystem.Models.Configurations
{
    using SingularSystem.Models.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class UserTermAcceptanceConfiguration : IEntityTypeConfiguration<UserTermAcceptance>
    {
        public void Configure(EntityTypeBuilder<UserTermAcceptance> builder)
        {
            builder.ToTable("user_term_acceptances");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.UserId, x.TermId }).IsUnique(); // um aceite por termo

            builder.Property(x => x.IpAddress).HasMaxLength(64);
            builder.Property(x => x.UserAgent).HasMaxLength(300);
            builder.Property(x => x.AcceptanceHash).IsRequired().HasMaxLength(64);

            builder.HasOne(x => x.Term)
                   .WithMany(t => t.Acceptances)
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
