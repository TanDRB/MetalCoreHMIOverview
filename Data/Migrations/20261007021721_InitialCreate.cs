using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalCoreHMIOverview.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MachineNo = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Metric = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Standard = table.Column<double>(type: "float", nullable: true),
                    Tolerance = table.Column<double>(type: "float", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: true),
                    IsGood = table.Column<bool>(type: "bit", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagReadings_TagDefinitions_TagDefinitionId",
                        column: x => x.TagDefinitionId,
                        principalTable: "TagDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagDefinitions_MachineNo_Section_Metric",
                table: "TagDefinitions",
                columns: new[] { "MachineNo", "Section", "Metric" });

            migrationBuilder.CreateIndex(
                name: "IX_TagDefinitions_Name",
                table: "TagDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagReadings_TagDefinitionId_TimestampUtc",
                table: "TagReadings",
                columns: new[] { "TagDefinitionId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagReadings");

            migrationBuilder.DropTable(
                name: "TagDefinitions");
        }
    }
}
