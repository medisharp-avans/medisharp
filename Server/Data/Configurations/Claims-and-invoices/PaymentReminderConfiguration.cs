using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data.Configurations
{
    public class PaymentReminderConfiguration : IEntityTypeConfiguration<PaymentReminder>
    {
        public void Configure(EntityTypeBuilder<PaymentReminder> builder)
        {
            builder.ToTable("payment_reminders", table =>
            {
                table.HasCheckConstraint("CK_payment_reminders_sequence_number", "[sequence_number] BETWEEN 1 AND 2");
                table.HasCheckConstraint("CK_payment_reminders_channel", DbEnum<ReminderChannel>.CheckSql("channel"));
            });

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id");
            builder.Property(r => r.InvoiceId).HasColumnName("invoice_id");
            builder.Property(r => r.SequenceNumber).HasColumnName("sequence_number");
            builder.Property(r => r.SentAt).HasColumnName("sent_at");
            builder.Property(r => r.Channel).HasColumnName("channel").HasMaxLength(10).HasConversion<string>();

            // An invoice gets each reminder (first, second) only once.
            builder.HasIndex(r => new { r.InvoiceId, r.SequenceNumber })
                .IsUnique()
                .HasDatabaseName("UX_payment_reminders_invoice_id_sequence_number");

            builder.HasOne(r => r.Invoice).WithMany(i => i.PaymentReminders).HasForeignKey(r => r.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
