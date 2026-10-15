using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class cuttingLengthForCommandCutting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CuttingLength",
                table: "CommandCuttingFiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuttingLength",
                table: "CommandCuttingFiles");
        }
    }
}
