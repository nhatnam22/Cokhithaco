using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMachineWorkShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandardTimeMinutes",
                table: "WorkShiftMachines");

            migrationBuilder.RenameColumn(
                name: "TimeDefault",
                table: "WorkShifts",
                newName: "StandardTimeMinutes");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkShiftId",
                table: "ShiftBreaks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StandardTimeMinutes",
                table: "WorkShifts",
                newName: "TimeDefault");

            migrationBuilder.AddColumn<decimal>(
                name: "StandardTimeMinutes",
                table: "WorkShiftMachines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkShiftId",
                table: "ShiftBreaks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
