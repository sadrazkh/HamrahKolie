using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamrahKolie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Features",
                table: "DialysisCenters",
                type: "integer",
                nullable: false,
                defaultValue: 19);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyPatientQuota",
                table: "DialysisCenters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "DialysisCenters");

            migrationBuilder.DropColumn(
                name: "MonthlyPatientQuota",
                table: "DialysisCenters");
        }
    }
}
