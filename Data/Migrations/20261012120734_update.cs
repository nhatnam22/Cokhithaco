using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MaterialFeedingPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "MaterialFeedingPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Thickness",
                table: "MaterialFeedingPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "MaterialFeedingPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CommandCuttingFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Note",
                table: "CommandCuttingFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "MaterialFeedingPlans");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "MaterialFeedingPlans");

            migrationBuilder.DropColumn(
                name: "Thickness",
                table: "MaterialFeedingPlans");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MaterialFeedingPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CommandCuttingFiles");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "CommandCuttingFiles");
        }
    }
}
