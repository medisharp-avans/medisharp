using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ModelConfigChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCalendarConnections");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLockedOut",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TreatmentCodes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedIcalUrl",
                table: "Specialists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedUtc",
                table: "Specialists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Urgency",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Regulier",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Referrals",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "HasInsurance",
                table: "Patients",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ExternalCalendarEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUid",
                table: "ExternalCalendarEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentCodes_Cost",
                table: "TreatmentCodes",
                sql: "[Cost] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TreatmentCodes_DurationInQuarters",
                table: "TreatmentCodes",
                sql: "[DurationInQuarters] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Bsn",
                table: "Patients",
                column: "Bsn",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Patients_Bsn",
                table: "Patients",
                sql: "LEN([Bsn]) = 9 AND [Bsn] NOT LIKE '%[^0-9]%'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExternalCalendarEvents_EndUtc",
                table: "ExternalCalendarEvents",
                sql: "[EndUtc] > [StartUtc]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointments_EndTime",
                table: "Appointments",
                sql: "[EndTime] > [StartTime]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentCodes_Cost",
                table: "TreatmentCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TreatmentCodes_DurationInQuarters",
                table: "TreatmentCodes");

            migrationBuilder.DropIndex(
                name: "IX_Patients_Bsn",
                table: "Patients");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Patients_Bsn",
                table: "Patients");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExternalCalendarEvents_EndUtc",
                table: "ExternalCalendarEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Appointments_EndTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TreatmentCodes");

            migrationBuilder.DropColumn(
                name: "EncryptedIcalUrl",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "LastSyncedUtc",
                table: "Specialists");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLockedOut",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Urgency",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Regulier");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Open");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Referrals",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "HasInsurance",
                table: "Patients",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ExternalCalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUid",
                table: "ExternalCalendarEvents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "UserCalendarConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecialistId = table.Column<int>(type: "int", nullable: false),
                    EncryptedIcalUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSyncedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCalendarConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCalendarConnections_Specialists_SpecialistId",
                        column: x => x.SpecialistId,
                        principalTable: "Specialists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCalendarConnections_SpecialistId",
                table: "UserCalendarConnections",
                column: "SpecialistId",
                unique: true);
        }
    }
}
