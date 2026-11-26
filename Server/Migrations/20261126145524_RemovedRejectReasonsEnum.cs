using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class RemovedRejectReasonsEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_claim_lines_rejection_reason",
                table: "claim_lines");

            migrationBuilder.AlterColumn<string>(
                name: "rejection_reason",
                table: "claim_lines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "rejection_reason",
                table: "claim_lines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_claim_lines_rejection_reason",
                table: "claim_lines",
                sql: "[rejection_reason] IS NULL OR [rejection_reason] IN (N'PolicyCondition', N'MaximumReached', N'InvalidPolicyNumber')");
        }
    }
}
