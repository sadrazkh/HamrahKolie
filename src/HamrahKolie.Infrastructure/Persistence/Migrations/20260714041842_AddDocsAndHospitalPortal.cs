using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamrahKolie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocsAndHospitalPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReferringCenterId",
                table: "SupportRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CenterId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ReferringCenterId",
                table: "SupportRequests",
                column: "ReferringCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_DialysisCenters_ReferringCenterId",
                table: "SupportRequests",
                column: "ReferringCenterId",
                principalTable: "DialysisCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_DialysisCenters_ReferringCenterId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_ReferringCenterId",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "ReferringCenterId",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "CenterId",
                table: "AspNetUsers");
        }
    }
}
