using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ModelChangesAndAdds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneralPractitioner_Users_UserId",
                table: "GeneralPractitioner");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_GeneralPractitioner_GeneralPractitionerId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_GeneralPractitioner_GeneralPractitionerId",
                table: "Referrals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralPractitioner",
                table: "GeneralPractitioner");

            migrationBuilder.RenameTable(
                name: "GeneralPractitioner",
                newName: "GeneralPractitioners");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralPractitioner_UserId",
                table: "GeneralPractitioners",
                newName: "IX_GeneralPractitioners_UserId");

            migrationBuilder.AlterColumn<int>(
                name: "Specialism",
                table: "Referrals",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralPractitioners",
                table: "GeneralPractitioners",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TreatmentCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Specialism = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationInQuarters = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCodes", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TreatmentCode",
                table: "Referrals",
                column: "TreatmentCode");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralPractitioners_Users_UserId",
                table: "GeneralPractitioners",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_GeneralPractitioners_GeneralPractitionerId",
                table: "Patients",
                column: "GeneralPractitionerId",
                principalTable: "GeneralPractitioners",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_GeneralPractitioners_GeneralPractitionerId",
                table: "Referrals",
                column: "GeneralPractitionerId",
                principalTable: "GeneralPractitioners",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_TreatmentCodes_TreatmentCode",
                table: "Referrals",
                column: "TreatmentCode",
                principalTable: "TreatmentCodes",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneralPractitioners_Users_UserId",
                table: "GeneralPractitioners");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_GeneralPractitioners_GeneralPractitionerId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_GeneralPractitioners_GeneralPractitionerId",
                table: "Referrals");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_TreatmentCodes_TreatmentCode",
                table: "Referrals");

            migrationBuilder.DropTable(
                name: "TreatmentCodes");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_TreatmentCode",
                table: "Referrals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralPractitioners",
                table: "GeneralPractitioners");

            migrationBuilder.RenameTable(
                name: "GeneralPractitioners",
                newName: "GeneralPractitioner");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralPractitioners_UserId",
                table: "GeneralPractitioner",
                newName: "IX_GeneralPractitioner_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Specialism",
                table: "Referrals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralPractitioner",
                table: "GeneralPractitioner",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralPractitioner_Users_UserId",
                table: "GeneralPractitioner",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_GeneralPractitioner_GeneralPractitionerId",
                table: "Patients",
                column: "GeneralPractitionerId",
                principalTable: "GeneralPractitioner",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_GeneralPractitioner_GeneralPractitionerId",
                table: "Referrals",
                column: "GeneralPractitionerId",
                principalTable: "GeneralPractitioner",
                principalColumn: "Id");
        }
    }
}
