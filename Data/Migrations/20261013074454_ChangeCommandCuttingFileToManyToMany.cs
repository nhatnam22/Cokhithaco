using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCommandCuttingFileToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // [DATA MIGRATION] Tạo bảng join trước
            migrationBuilder.CreateTable(
                name: "CommandCuttingFileProductionCommand",
                columns: table => new
                {
                    CommandCuttingFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionCommandsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandCuttingFileProductionCommand", x => new { x.CommandCuttingFileId, x.ProductionCommandsId });
                    table.ForeignKey(
                        name: "FK_CommandCuttingFileProductionCommand_CommandCuttingFiles_CommandCuttingFileId",
                        column: x => x.CommandCuttingFileId,
                        principalTable: "CommandCuttingFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommandCuttingFileProductionCommand_ProductionCommands_ProductionCommandsId",
                        column: x => x.ProductionCommandsId,
                        principalTable: "ProductionCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // [DATA MIGRATION] Chuyển dữ liệu cũ từ cột ProductionCommandId vào bảng join
            // Mỗi dòng CommandCuttingFile hiện tại có 1 ProductionCommandId → insert vào bảng join
            migrationBuilder.Sql(@"
                INSERT INTO CommandCuttingFileProductionCommand (CommandCuttingFileId, ProductionCommandsId)
                SELECT Id, ProductionCommandId FROM CommandCuttingFiles WHERE ProductionCommandId IS NOT NULL;
            ");

            // [DATA MIGRATION] Xóa FK cũ, index cũ, và cột ProductionCommandId
            migrationBuilder.DropForeignKey(
                name: "FK_CommandCuttingFiles_ProductionCommands_ProductionCommandId",
                table: "CommandCuttingFiles");

            migrationBuilder.DropIndex(
                name: "IX_CommandCuttingFiles_CuttingFileId_ProductionCommandId",
                table: "CommandCuttingFiles");

            migrationBuilder.DropIndex(
                name: "IX_CommandCuttingFiles_ProductionCommandId",
                table: "CommandCuttingFiles");

            migrationBuilder.DropColumn(
                name: "ProductionCommandId",
                table: "CommandCuttingFiles");

            // [THAY ĐỔI] Index mới chỉ trên CuttingFileId (1 CCF = 1 CuttingFile)
            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_CuttingFileId",
                table: "CommandCuttingFiles",
                column: "CuttingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFileProductionCommand_ProductionCommandsId",
                table: "CommandCuttingFileProductionCommand",
                column: "ProductionCommandsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandCuttingFileProductionCommand");

            migrationBuilder.DropIndex(
                name: "IX_CommandCuttingFiles_CuttingFileId",
                table: "CommandCuttingFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionCommandId",
                table: "CommandCuttingFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_CuttingFileId_ProductionCommandId",
                table: "CommandCuttingFiles",
                columns: new[] { "CuttingFileId", "ProductionCommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_ProductionCommandId",
                table: "CommandCuttingFiles",
                column: "ProductionCommandId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandCuttingFiles_ProductionCommands_ProductionCommandId",
                table: "CommandCuttingFiles",
                column: "ProductionCommandId",
                principalTable: "ProductionCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
