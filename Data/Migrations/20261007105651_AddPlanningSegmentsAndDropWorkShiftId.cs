using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningSegmentsAndDropWorkShiftId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plannings_WorkShifts_WorkShiftId",
                table: "Plannings");

            migrationBuilder.DropIndex(
                name: "IX_Plannings_WorkShiftId",
                table: "Plannings");

            migrationBuilder.DropColumn(
                name: "WorkShiftId",
                table: "Plannings");

            migrationBuilder.CreateTable(
                name: "PlanningSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanningId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningSegments_Plannings_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Plannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSegments_PlanningId",
                table: "PlanningSegments",
                column: "PlanningId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanningSegments");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkShiftId",
                table: "Plannings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plannings_WorkShiftId",
                table: "Plannings",
                column: "WorkShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plannings_WorkShifts_WorkShiftId",
                table: "Plannings",
                column: "WorkShiftId",
                principalTable: "WorkShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
