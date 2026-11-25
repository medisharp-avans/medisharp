using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospital_employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_hospital_employees_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    paid_at = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.CheckConstraint("CK_invoices_due_date", "[due_date] >= [invoice_date]");
                    table.CheckConstraint("CK_invoices_reason", "[reason] IN (N'NoInsurer', N'LateCancellation', N'NotReimbursed')");
                    table.CheckConstraint("CK_invoices_status", "[status] IN (N'Open', N'Paid', N'SentToCollection', N'WrittenOff')");
                });

            migrationBuilder.CreateTable(
                name: "claim_batches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sent_by_id = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_claim_batches_hospital_employees_sent_by_id",
                        column: x => x.sent_by_id,
                        principalTable: "hospital_employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<int>(type: "int", nullable: false),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.id);
                    table.CheckConstraint("CK_invoice_lines_amount", "[amount] >= 0");
                    table.ForeignKey(
                        name: "FK_invoice_lines_Appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<int>(type: "int", nullable: false),
                    number_of_installments = table.Column<int>(type: "int", nullable: false),
                    installment_amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    terms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_plans", x => x.id);
                    table.CheckConstraint("CK_payment_plans_installment_amount", "[installment_amount] > 0");
                    table.CheckConstraint("CK_payment_plans_number_of_installments", "[number_of_installments] > 0");
                    table.ForeignKey(
                        name: "FK_payment_plans_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment_reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<int>(type: "int", nullable: false),
                    sequence_number = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    channel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_reminders", x => x.id);
                    table.CheckConstraint("CK_payment_reminders_channel", "[channel] IN (N'Email', N'Phone')");
                    table.CheckConstraint("CK_payment_reminders_sequence_number", "[sequence_number] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_payment_reminders_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    batch_id = table.Column<int>(type: "int", nullable: true),
                    reviewed_by_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    policy_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.id);
                    table.CheckConstraint("CK_claims_status", "[status] IN (N'ToReview', N'ReadyToSend', N'Processed', N'HandledManually')");
                    table.CheckConstraint("CK_claims_submitted_at", "[submitted_at] IS NULL OR [submitted_at] >= [created_at]");
                    table.ForeignKey(
                        name: "FK_claims_Patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_claims_claim_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "claim_batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_claims_hospital_employees_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "hospital_employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "claim_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    claim_id = table.Column<int>(type: "int", nullable: false),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    submitted_amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    reimbursed_amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    submission_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_lines", x => x.id);
                    table.CheckConstraint("CK_claim_lines_reimbursed_amount", "[reimbursed_amount] IS NULL OR [reimbursed_amount] BETWEEN 0 AND [submitted_amount]");
                    table.CheckConstraint("CK_claim_lines_rejection_reason", "[rejection_reason] IS NULL OR [rejection_reason] IN (N'PolicyCondition', N'MaximumReached', N'InvalidPolicyNumber')");
                    table.CheckConstraint("CK_claim_lines_status", "[status] IN (N'Pending', N'Reimbursed', N'PartiallyReimbursed', N'Rejected')");
                    table.CheckConstraint("CK_claim_lines_submission_count", "[submission_count] >= 0");
                    table.CheckConstraint("CK_claim_lines_submitted_amount", "[submitted_amount] >= 0");
                    table.ForeignKey(
                        name: "FK_claim_lines_Appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_claim_lines_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_claim_batches_sent_by_id",
                table: "claim_batches",
                column: "sent_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_lines_claim_id",
                table: "claim_lines",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "UX_claim_lines_appointment_id",
                table: "claim_lines",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claims_batch_id",
                table: "claims",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_patient_id",
                table: "claims",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_reviewed_by_id",
                table: "claims",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "UX_hospital_employees_user_id",
                table: "hospital_employees",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_invoice_id",
                table: "invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "UX_invoice_lines_appointment_id",
                table: "invoice_lines",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_plans_invoice_id",
                table: "payment_plans",
                column: "invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_payment_reminders_invoice_id_sequence_number",
                table: "payment_reminders",
                columns: new[] { "invoice_id", "sequence_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim_lines");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "payment_plans");

            migrationBuilder.DropTable(
                name: "payment_reminders");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "claim_batches");

            migrationBuilder.DropTable(
                name: "hospital_employees");
        }
    }
}
