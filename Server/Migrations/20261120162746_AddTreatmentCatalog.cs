using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Treatments",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationInQuarters = table.Column<int>(type: "int", nullable: false),
                    CostEuro = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treatments", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "Treatments",
                columns: new[] { "Code", "CostEuro", "Description", "DurationInQuarters", "Specialty" },
                values: new object[,]
                {
                    { "Z0001", 144m, "Consult hart", 3, "Cardiologie" },
                    { "Z0002", 51m, "Consult huid", 3, "Dermatologie" },
                    { "Z0003", 117m, "Consult longen", 2, "Longgeneeskunde" },
                    { "Z0004", 114m, "Consult ogen", 3, "Oogheelkunde" },
                    { "Z0005", 65m, "Consult oren", 3, "KNO" },
                    { "Z0006", 1084m, "Operatie knie", 2, "Orthopedie" },
                    { "Z0007", 2909m, "Operatie heup", 3, "Orthopedie" },
                    { "Z0008", 2572m, "Operatie hart", 4, "Cardiologie" },
                    { "Z0009", 2739m, "Operatie huid", 4, "Chirurgie" },
                    { "Z0010", 2903m, "Operatie rug", 3, "Orthopedie" },
                    { "Z0011", 187m, "Controle hart", 3, "Cardiologie" },
                    { "Z0012", 172m, "Controle huid", 4, "Dermatologie" },
                    { "Z0013", 72m, "Controle longen", 3, "Longgeneeskunde" },
                    { "Z0014", 96m, "Controle ogen", 3, "Oogheelkunde" },
                    { "Z0015", 150m, "Controle oren", 4, "KNO" },
                    { "Z0016", 609m, "MRI-scan hoofd", 2, "Radiologie" },
                    { "Z0017", 223m, "MRI-scan knie", 3, "Radiologie" },
                    { "Z0018", 314m, "MRI-scan rug", 3, "Radiologie" },
                    { "Z0019", 728m, "MRI-scan hart", 2, "Radiologie" },
                    { "Z0020", 395m, "MRI-scan buik", 2, "Radiologie" },
                    { "Z0021", 682m, "CT-scan hoofd", 3, "Radiologie" },
                    { "Z0022", 616m, "CT-scan knie", 3, "Radiologie" },
                    { "Z0023", 378m, "CT-scan rug", 3, "Radiologie" },
                    { "Z0024", 793m, "CT-scan hart", 3, "Radiologie" },
                    { "Z0025", 448m, "CT-scan buik", 4, "Radiologie" },
                    { "Z0026", 55m, "Bloedonderzoek algemeen", 1, "Laboratoriumgeneeskunde" },
                    { "Z0027", 92m, "Bloedonderzoek hormonen", 2, "Laboratoriumgeneeskunde" },
                    { "Z0028", 71m, "Bloedonderzoek infecties", 1, "Laboratoriumgeneeskunde" },
                    { "Z0029", 98m, "Bloedonderzoek suiker", 4, "Laboratoriumgeneeskunde" },
                    { "Z0030", 38m, "Bloedonderzoek cholesterol", 3, "Laboratoriumgeneeskunde" },
                    { "Z0031", 43m, "Fysiotherapie knie", 1, "Revalidatiegeneeskunde" },
                    { "Z0032", 70m, "Fysiotherapie rug", 3, "Revalidatiegeneeskunde" },
                    { "Z0033", 101m, "Fysiotherapie schouder", 2, "Revalidatiegeneeskunde" },
                    { "Z0034", 103m, "Fysiotherapie heup", 4, "Revalidatiegeneeskunde" },
                    { "Z0035", 93m, "Fysiotherapie nek", 1, "Revalidatiegeneeskunde" },
                    { "Z0036", 123m, "Psychologisch gesprek depressie", 4, "Psychiatrie" },
                    { "Z0037", 132m, "Psychologisch gesprek angst", 3, "Psychiatrie" },
                    { "Z0038", 72m, "Psychologisch gesprek trauma", 1, "Psychiatrie" },
                    { "Z0039", 172m, "Psychologisch gesprek burn-out", 3, "Psychiatrie" },
                    { "Z0040", 135m, "Psychologisch gesprek stress", 4, "Psychiatrie" },
                    { "Z0041", 32m, "Vaccinatie griep", 3, "Algemeen" },
                    { "Z0042", 57m, "Vaccinatie hepatitis", 2, "Algemeen" },
                    { "Z0043", 61m, "Vaccinatie tetanus", 1, "Algemeen" },
                    { "Z0044", 45m, "Vaccinatie covid", 2, "Algemeen" },
                    { "Z0045", 30m, "Vaccinatie HPV", 4, "Algemeen" },
                    { "Z0046", 55m, "Wondverzorging arm", 2, "Chirurgie" },
                    { "Z0047", 111m, "Wondverzorging been", 2, "Chirurgie" },
                    { "Z0048", 33m, "Wondverzorging hoofd", 2, "Chirurgie" },
                    { "Z0049", 56m, "Wondverzorging buik", 4, "Chirurgie" },
                    { "Z0050", 103m, "Wondverzorging rug", 4, "Chirurgie" },
                    { "Z0051", 100m, "Fysiotherapie rug", 1, "Revalidatiegeneeskunde" },
                    { "Z0052", 79m, "Fysiotherapie schouder", 4, "Revalidatiegeneeskunde" },
                    { "Z0053", 130m, "Wondverzorging rug", 4, "Chirurgie" },
                    { "Z0054", 76m, "Vaccinatie HPV", 4, "Algemeen" },
                    { "Z0055", 59m, "Vaccinatie hepatitis", 1, "Algemeen" },
                    { "Z0056", 105m, "Psychologisch gesprek stress", 1, "Psychiatrie" },
                    { "Z0057", 126m, "Consult ogen", 1, "Oogheelkunde" },
                    { "Z0058", 2829m, "Operatie rug", 1, "Orthopedie" },
                    { "Z0059", 222m, "MRI-scan buik", 2, "Radiologie" },
                    { "Z0060", 69m, "Controle huid", 3, "Dermatologie" },
                    { "Z0061", 616m, "MRI-scan hoofd", 1, "Radiologie" },
                    { "Z0062", 181m, "Controle longen", 4, "Longgeneeskunde" },
                    { "Z0063", 68m, "Bloedonderzoek algemeen", 4, "Laboratoriumgeneeskunde" },
                    { "Z0064", 65m, "Wondverzorging arm", 2, "Chirurgie" },
                    { "Z0065", 1272m, "Operatie hart", 4, "Cardiologie" },
                    { "Z0066", 72m, "Consult huid", 2, "Dermatologie" },
                    { "Z0067", 64m, "Psychologisch gesprek angst", 4, "Psychiatrie" },
                    { "Z0068", 719m, "CT-scan knie", 1, "Radiologie" },
                    { "Z0069", 96m, "Wondverzorging been", 1, "Chirurgie" },
                    { "Z0070", 2818m, "Operatie huid", 4, "Chirurgie" },
                    { "Z0071", 73m, "Consult hart", 2, "Cardiologie" },
                    { "Z0072", 110m, "Consult oren", 4, "KNO" },
                    { "Z0073", 42m, "Bloedonderzoek cholesterol", 3, "Laboratoriumgeneeskunde" },
                    { "Z0074", 192m, "Controle hart", 4, "Cardiologie" },
                    { "Z0075", 80m, "Vaccinatie griep", 4, "Algemeen" },
                    { "Z0076", 482m, "CT-scan rug", 4, "Radiologie" },
                    { "Z0077", 52m, "Bloedonderzoek hormonen", 2, "Laboratoriumgeneeskunde" },
                    { "Z0078", 47m, "Vaccinatie covid", 2, "Algemeen" },
                    { "Z0079", 104m, "Psychologisch gesprek trauma", 1, "Psychiatrie" },
                    { "Z0080", 781m, "MRI-scan hart", 2, "Radiologie" },
                    { "Z0081", 118m, "Fysiotherapie heup", 2, "Revalidatiegeneeskunde" },
                    { "Z0082", 441m, "MRI-scan knie", 1, "Radiologie" },
                    { "Z0083", 184m, "Psychologisch gesprek burn-out", 4, "Psychiatrie" },
                    { "Z0084", 453m, "CT-scan buik", 2, "Radiologie" },
                    { "Z0085", 76m, "Consult longen", 2, "Longgeneeskunde" },
                    { "Z0086", 572m, "CT-scan hoofd", 4, "Radiologie" },
                    { "Z0087", 69m, "Fysiotherapie knie", 1, "Revalidatiegeneeskunde" },
                    { "Z0088", 66m, "Wondverzorging buik", 4, "Chirurgie" },
                    { "Z0089", 598m, "MRI-scan rug", 4, "Radiologie" },
                    { "Z0090", 159m, "Psychologisch gesprek depressie", 2, "Psychiatrie" },
                    { "Z0091", 1128m, "Operatie knie", 3, "Orthopedie" },
                    { "Z0092", 102m, "Fysiotherapie nek", 4, "Revalidatiegeneeskunde" },
                    { "Z0093", 501m, "CT-scan hart", 1, "Radiologie" },
                    { "Z0094", 167m, "Controle ogen", 2, "Oogheelkunde" },
                    { "Z0095", 48m, "Bloedonderzoek suiker", 1, "Laboratoriumgeneeskunde" },
                    { "Z0096", 138m, "Controle oren", 1, "KNO" },
                    { "Z0097", 46m, "Wondverzorging hoofd", 1, "Chirurgie" },
                    { "Z0098", 46m, "Vaccinatie tetanus", 1, "Algemeen" },
                    { "Z0099", 738m, "Operatie heup", 1, "Orthopedie" },
                    { "Z0100", 37m, "Bloedonderzoek infecties", 4, "Laboratoriumgeneeskunde" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_Specialty",
                table: "Treatments",
                column: "Specialty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Treatments");
        }
    }
}
