using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_SpecialistId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_SpecialistId_StartTime",
                table: "Appointments",
                columns: new[] { "SpecialistId", "StartTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_SpecialistId_StartTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_SpecialistId",
                table: "Appointments",
                column: "SpecialistId");
        }
    }
}
