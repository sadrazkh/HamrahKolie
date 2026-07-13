using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HamrahKolie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteersAndCenters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialysisCenters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Services = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Facilities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DialysisTypes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AccessibilityNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedByPublic = table.Column<bool>(type: "boolean", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialysisCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volunteers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CollaborationType = table.Column<int>(type: "integer", nullable: false),
                    Skills = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AvailableTimes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Background = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    ConsentAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volunteers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialysisCenters_IsApproved",
                table: "DialysisCenters",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_DialysisCenters_Province",
                table: "DialysisCenters",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_DialysisCenters_Slug",
                table: "DialysisCenters",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Mobile",
                table: "Volunteers",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Status",
                table: "Volunteers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialysisCenters");

            migrationBuilder.DropTable(
                name: "Volunteers");
        }
    }
}
