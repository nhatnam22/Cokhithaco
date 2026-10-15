using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialFeedingPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryCuttingFileId",
                table: "CommandCuttingFiles",
                type: "uniqueidentifier",
                nullable: true,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MaterialFeedingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanningId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    QuantityActual = table.Column<int>(type: "int", nullable: false),
                    TimeAtCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_MaterialFeedingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialFeedingPlans_Plannings_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Plannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialFeedingPlans_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialFeedingPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_CategoryCuttingFileId",
                table: "CommandCuttingFiles",
                column: "CategoryCuttingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFeedingPlans_CreatedBy",
                table: "MaterialFeedingPlans",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFeedingPlans_PlanningId",
                table: "MaterialFeedingPlans",
                column: "PlanningId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFeedingPlans_UserId",
                table: "MaterialFeedingPlans",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandCuttingFiles_CategoryCuttingFiles_CategoryCuttingFileId",
                table: "CommandCuttingFiles",
                column: "CategoryCuttingFileId",
                principalTable: "CategoryCuttingFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandCuttingFiles_CategoryCuttingFiles_CategoryCuttingFileId",
                table: "CommandCuttingFiles");

            migrationBuilder.DropTable(
                name: "MaterialFeedingPlans");

            migrationBuilder.DropIndex(
                name: "IX_CommandCuttingFiles_CategoryCuttingFileId",
                table: "CommandCuttingFiles");

            migrationBuilder.DropColumn(
                name: "CategoryCuttingFileId",
                table: "CommandCuttingFiles");
        }
    }
}
