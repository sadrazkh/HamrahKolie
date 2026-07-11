using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HamrahKolie.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPageSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PageSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ButtonText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ButtonUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SecondaryButtonText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SecondaryButtonUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    Background = table.Column<int>(type: "integer", nullable: false),
                    Padding = table.Column<int>(type: "integer", nullable: false),
                    ShowOnMobile = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnDesktop = table.Column<bool>(type: "boolean", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_PageSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageSections_MediaFiles_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_ImageId",
                table: "PageSections",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_PageKey_SortOrder",
                table: "PageSections",
                columns: new[] { "PageKey", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageSections");
        }
    }
}
