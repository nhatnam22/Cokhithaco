using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeMachinev2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CutThicknessMm",
                table: "Machines",
                newName: "MinCutThicknessMm");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxCutThicknessMm",
                table: "Machines",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCutThicknessMm",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "MinCutThicknessMm",
                table: "Machines",
                newName: "CutThicknessMm");
        }
    }
}
