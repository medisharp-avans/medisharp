using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoices", table =>
            {
                table.HasCheckConstraint("CK_invoices_due_date", "[due_date] >= [invoice_date]");
                table.HasCheckConstraint("CK_invoices_reason", DbEnum<InvoiceReason>.CheckSql("reason"));
                table.HasCheckConstraint("CK_invoices_status", DbEnum<InvoiceStatus>.CheckSql("status"));
            });

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnName("id");
            builder.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(20);
            builder.Property(i => i.Reason).HasColumnName("reason").HasMaxLength(30).HasConversion<string>();
            builder.Property(i => i.InvoiceDate).HasColumnName("invoice_date");
            builder.Property(i => i.DueDate).HasColumnName("due_date");
            builder.Property(i => i.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>();
            builder.Property(i => i.PaidAt).HasColumnName("paid_at");

            builder.HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasDatabaseName("UX_invoices_invoice_number");
        }
    }
}
