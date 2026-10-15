using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeMachine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CutLengthMm",
                table: "Machines",
                newName: "MinCutLengthMm");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxCutLengthMm",
                table: "Machines",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCutLengthMm",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "MinCutLengthMm",
                table: "Machines",
                newName: "CutLengthMm");
        }
    }
}
