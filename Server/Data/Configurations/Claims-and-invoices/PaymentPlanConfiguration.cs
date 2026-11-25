using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Models;

namespace Server.Data.Configurations
{
    public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
    {
        public void Configure(EntityTypeBuilder<PaymentPlan> builder)
        {
            builder.ToTable("payment_plans", table =>
            {
                table.HasCheckConstraint("CK_payment_plans_number_of_installments", "[number_of_installments] > 0");
                table.HasCheckConstraint("CK_payment_plans_installment_amount", "[installment_amount] > 0");
            });

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.InvoiceId).HasColumnName("invoice_id");
            builder.Property(p => p.NumberOfInstallments).HasColumnName("number_of_installments");
            builder.Property(p => p.InstallmentAmount).HasColumnName("installment_amount").HasPrecision(10, 2);
            builder.Property(p => p.StartDate).HasColumnName("start_date");
            builder.Property(p => p.Terms).HasColumnName("terms").HasMaxLength(500);

            // At most one plan per invoice (the one-to-one makes invoice_id unique).
            builder.HasOne(p => p.Invoice).WithOne(i => i.PaymentPlan).HasForeignKey<PaymentPlan>(p => p.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
