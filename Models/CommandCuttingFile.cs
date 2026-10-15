using CoKhiThaCo.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class CommandCuttingFile : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CuttingFileId { get; set; }
        [ForeignKey("CuttingFileId")]
        public CuttingFile CuttingFile { get; set; }

        public Guid? CategoryCuttingFileId { get; set; }
        [ForeignKey("CategoryCuttingFileId")]
        public CategoryCuttingFile CategoryCuttingFile { get; set; }

        // [THAY ĐỔI NHIỆP VỤ] 1 CommandCuttingFile = 1 CuttingFile + Nhiều ProductionCommand
        // Xóa: ProductionCommandId (FK đơn) và ProductionCommand (nav đơn)
        // Thay bằng: ICollection<ProductionCommand> (many-to-many, EF tự tạo bảng join)
        public ICollection<ProductionCommand> ProductionCommands { get; set; }

        public Priority Priority { get; set; } = Priority.Medium;

        [MaxLength(100)]
        public string MaterialCode { get; set; }

        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }

        public int Quantity { get; set; }
        public decimal CuttingLength { get; set; }

        public decimal CuttingTime { get; set; }

        public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Created;

        public int PlannedQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public NoteType Note { get; set; }
        public string Description { get; set; }
        public decimal RemainLength { get; set; }
        public decimal RemainWidth { get; set; }
        public decimal RemainThickness { get; set; }
    }
}
