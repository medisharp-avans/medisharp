using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations;

public partial class AddPatientsToReferrals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Bsn = table.Column<string>(type: "varchar(9)", unicode: false, maxLength: 9, nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PostalCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Patients", row => row.Id));

        migrationBuilder.AddColumn<int>("PatientId", "Referrals", "int", nullable: true);
        migrationBuilder.CreateIndex("IX_Patients_Bsn", "Patients", "Bsn", unique: true);
        migrationBuilder.CreateIndex("IX_Referrals_PatientId", "Referrals", "PatientId");
        migrationBuilder.AddForeignKey("FK_Referrals_Patients_PatientId", "Referrals", "PatientId",
            "Patients", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Referrals_Patients_PatientId", "Referrals");
        migrationBuilder.DropIndex("IX_Referrals_PatientId", "Referrals");
        migrationBuilder.DropColumn("PatientId", "Referrals");
        migrationBuilder.DropTable("Patients");
    }
}
