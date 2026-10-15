using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoKhiThaCo.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_PowerCapacities_PowerId",
                table: "Machines");

            migrationBuilder.DropForeignKey(
                name: "FK_Plannings_ProductionOrders_ProductionOrderId",
                table: "Plannings");

            migrationBuilder.DropTable(
                name: "PowerCapacities");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_Machines_PowerId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "PowerId",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "ProductionOrderId",
                table: "Plannings",
                newName: "CommandCuttingFileId");

            migrationBuilder.RenameIndex(
                name: "IX_Plannings_ProductionOrderId",
                table: "Plannings",
                newName: "IX_Plannings_CommandCuttingFileId");

            migrationBuilder.AddColumn<decimal>(
                name: "CutLengthMm",
                table: "Machines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CutThicknessMm",
                table: "Machines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerformancePercent",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PowerKw",
                table: "Machines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CategoryCuttingFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CategoryCuttingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryCuttingFiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OrderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ProductionCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionCommands_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuttingFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryCuttingFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CuttingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuttingFiles_CategoryCuttingFiles_CategoryCuttingFileId",
                        column: x => x.CategoryCuttingFileId,
                        principalTable: "CategoryCuttingFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuttingFiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineCategoryCuttingFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryCuttingFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_MachineCategoryCuttingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineCategoryCuttingFiles_CategoryCuttingFiles_CategoryCuttingFileId",
                        column: x => x.CategoryCuttingFileId,
                        principalTable: "CategoryCuttingFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineCategoryCuttingFiles_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "MachineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineCategoryCuttingFiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandCuttingFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuttingFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionCommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    MaterialCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Length = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Thickness = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CuttingTime = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CommandCuttingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandCuttingFiles_CuttingFiles_CuttingFileId",
                        column: x => x.CuttingFileId,
                        principalTable: "CuttingFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommandCuttingFiles_ProductionCommands_ProductionCommandId",
                        column: x => x.ProductionCommandId,
                        principalTable: "ProductionCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommandCuttingFiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryCuttingFiles_Code",
                table: "CategoryCuttingFiles",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryCuttingFiles_CreatedBy",
                table: "CategoryCuttingFiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_CreatedBy",
                table: "CommandCuttingFiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_CuttingFileId_ProductionCommandId",
                table: "CommandCuttingFiles",
                columns: new[] { "CuttingFileId", "ProductionCommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandCuttingFiles_ProductionCommandId",
                table: "CommandCuttingFiles",
                column: "ProductionCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingFiles_CategoryCuttingFileId",
                table: "CuttingFiles",
                column: "CategoryCuttingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingFiles_Code",
                table: "CuttingFiles",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingFiles_CreatedBy",
                table: "CuttingFiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCategoryCuttingFiles_CategoryCuttingFileId",
                table: "MachineCategoryCuttingFiles",
                column: "CategoryCuttingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCategoryCuttingFiles_CreatedBy",
                table: "MachineCategoryCuttingFiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCategoryCuttingFiles_MachineId_CategoryCuttingFileId",
                table: "MachineCategoryCuttingFiles",
                columns: new[] { "MachineId", "CategoryCuttingFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCommands_CommandCode",
                table: "ProductionCommands",
                column: "CommandCode",
                unique: true,
                filter: "[CommandCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCommands_CreatedBy",
                table: "ProductionCommands",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Plannings_CommandCuttingFiles_CommandCuttingFileId",
                table: "Plannings",
                column: "CommandCuttingFileId",
                principalTable: "CommandCuttingFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plannings_CommandCuttingFiles_CommandCuttingFileId",
                table: "Plannings");

            migrationBuilder.DropTable(
                name: "CommandCuttingFiles");

            migrationBuilder.DropTable(
                name: "MachineCategoryCuttingFiles");

            migrationBuilder.DropTable(
                name: "CuttingFiles");

            migrationBuilder.DropTable(
                name: "ProductionCommands");

            migrationBuilder.DropTable(
                name: "CategoryCuttingFiles");

            migrationBuilder.DropColumn(
                name: "CutLengthMm",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CutThicknessMm",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "PerformancePercent",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "PowerKw",
                table: "Machines");

            migrationBuilder.RenameColumn(
                name: "CommandCuttingFileId",
                table: "Plannings",
                newName: "ProductionOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Plannings_CommandCuttingFileId",
                table: "Plannings",
                newName: "IX_Plannings_ProductionOrderId");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Machines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PowerId",
                table: "Machines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PowerCapacities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CutLengthMm = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerCapacities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PowerCapacities_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CutFileCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CuttingTime = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Length = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaterialCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Thickness = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Machines_PowerId",
                table: "Machines",
                column: "PowerId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerCapacities_CreatedBy",
                table: "PowerCapacities",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CreatedBy",
                table: "ProductionOrders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductionOrderCode",
                table: "ProductionOrders",
                column: "ProductionOrderCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_PowerCapacities_PowerId",
                table: "Machines",
                column: "PowerId",
                principalTable: "PowerCapacities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Plannings_ProductionOrders_ProductionOrderId",
                table: "Plannings",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
