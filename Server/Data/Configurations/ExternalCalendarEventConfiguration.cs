
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class ExternalCalendarEventConfiguration : IEntityTypeConfiguration<ExternalCalendarEvent>
    {
        public void Configure(EntityTypeBuilder<ExternalCalendarEvent> builder)
        {
            builder.HasKey(ev => ev.Id);

            builder.Property(ev => ev.SpecialistId)
                .IsRequired();

            builder.HasOne(ev => ev.Specialist)
                .WithMany()
                .HasForeignKey(ev => ev.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(ev => ev.ExternalUid)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ev => ev.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(ev => new { ev.SpecialistId, ev.ExternalUid })
                .IsUnique();

            builder.Property(ev => ev.StartUtc)
                .IsRequired();

            builder.Property(ev => ev.EndUtc)
                .IsRequired();

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_ExternalCalendarEvents_EndUtc",
                "[EndUtc] > [StartUtc]"));

            builder.Property(ev => ev.IsAllDay)
                .IsRequired();
        }
    }
}
