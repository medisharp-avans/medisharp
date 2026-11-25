using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("invoice_lines", table =>
            {
                table.HasCheckConstraint("CK_invoice_lines_amount", "[amount] >= 0");
            });

            builder.HasKey(il => il.Id);
            builder.Property(il => il.Id).HasColumnName("id");
            builder.Property(il => il.InvoiceId).HasColumnName("invoice_id");
            builder.Property(il => il.AppointmentId).HasColumnName("appointment_id");
            builder.Property(il => il.Amount).HasColumnName("amount").HasPrecision(10, 2);

            // An appointment is invoiced at most once.
            builder.HasIndex(il => il.AppointmentId)
                .IsUnique()
                .HasDatabaseName("UX_invoice_lines_appointment_id");

            builder.HasOne(il => il.Invoice).WithMany(i => i.InvoiceLines).HasForeignKey(il => il.InvoiceId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(il => il.Appointment).WithOne().HasForeignKey<InvoiceLine>(il => il.AppointmentId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
