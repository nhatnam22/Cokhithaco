using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftBreakAndPlanningWorkShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StandardTimeMinutes",
                table: "WorkShiftMachines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkShiftId",
                table: "Plannings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReserveMachine",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShiftBreaks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreakName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftBreaks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftBreaks_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plannings_WorkShiftId",
                table: "Plannings",
                column: "WorkShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBreaks_CreatedBy",
                table: "ShiftBreaks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftBreaks_WorkShiftId",
                table: "ShiftBreaks",
                column: "WorkShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plannings_WorkShifts_WorkShiftId",
                table: "Plannings",
                column: "WorkShiftId",
                principalTable: "WorkShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plannings_WorkShifts_WorkShiftId",
                table: "Plannings");

            migrationBuilder.DropTable(
                name: "ShiftBreaks");

            migrationBuilder.DropIndex(
                name: "IX_Plannings_WorkShiftId",
                table: "Plannings");

            migrationBuilder.DropColumn(
                name: "StandardTimeMinutes",
                table: "WorkShiftMachines");

            migrationBuilder.DropColumn(
                name: "WorkShiftId",
                table: "Plannings");

            migrationBuilder.DropColumn(
                name: "IsReserveMachine",
                table: "Machines");
        }
    }
}
