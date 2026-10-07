using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalCoreHMIOverview.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTagScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Scale",
                table: "TagDefinitions",
                type: "float",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scale",
                table: "TagDefinitions");
        }
    }
}
