using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations;

public partial class AddReferralPostalCodeAndHouseNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>("PatientAddress", "Referrals", "nvarchar(300)",
            maxLength: 300, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(300)", oldMaxLength: 300);
        migrationBuilder.AlterColumn<string>("PatientCity", "Referrals", "nvarchar(100)",
            maxLength: 100, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
        migrationBuilder.AddColumn<string>("PatientPostalCode", "Referrals", "nvarchar(12)", maxLength: 12, nullable: true);
        migrationBuilder.AddColumn<string>("PatientHouseNumber", "Referrals", "nvarchar(20)", maxLength: 20, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("PatientPostalCode", "Referrals");
        migrationBuilder.DropColumn("PatientHouseNumber", "Referrals");
        migrationBuilder.AlterColumn<string>("PatientAddress", "Referrals", "nvarchar(300)",
            maxLength: 300, nullable: false, defaultValue: "", oldClrType: typeof(string),
            oldType: "nvarchar(300)", oldMaxLength: 300, oldNullable: true);
        migrationBuilder.AlterColumn<string>("PatientCity", "Referrals", "nvarchar(100)",
            maxLength: 100, nullable: false, defaultValue: "", oldClrType: typeof(string),
            oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
    }
}
