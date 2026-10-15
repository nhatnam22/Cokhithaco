using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeMachinev3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TimeDefault",
                table: "WorkShifts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeDefault",
                table: "WorkShifts");
        }
    }
}
