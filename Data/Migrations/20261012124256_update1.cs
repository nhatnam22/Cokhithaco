using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainLength",
                table: "CommandCuttingFiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainThickness",
                table: "CommandCuttingFiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainWidth",
                table: "CommandCuttingFiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainLength",
                table: "CommandCuttingFiles");

            migrationBuilder.DropColumn(
                name: "RemainThickness",
                table: "CommandCuttingFiles");

            migrationBuilder.DropColumn(
                name: "RemainWidth",
                table: "CommandCuttingFiles");
        }
    }
}
