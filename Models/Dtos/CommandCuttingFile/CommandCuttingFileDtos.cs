using System;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models.Dtos.CommandCuttingFile
{
    public class CommandCuttingFileDto
    {
        public Guid Id { get; set; }
        public int Priority { get; set; }
        public string PriorityName { get; set; }
        public Guid CuttingFileId { get; set; }
        public string CuttingFileCode { get; set; }
        public string CuttingFileName { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public string CategoryCuttingFileName { get; set; }
        public List<CommandProductionCommandDto> ProductionCommands { get; set; }
        public string MaterialCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }
        public int Quantity { get; set; }
        public decimal CuttingLength { get; set; }
        public decimal CuttingTime { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public int PlannedQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public NoteType Note { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal RemainLength { get; set; }
        public decimal RemainWidth { get; set; }
        public decimal RemainThickness { get; set; }
        public string UserName { get; set; }
    }
    public class CommandProductionCommandDto {
        public Guid ProductionCommandId { get; set; }
        public string ProductionCommandCode { get; set; }
        public string OrderName { get; set; }
    }

    public class CreateCommandCuttingFileRequest
    {
        public int Priority { get; set; } = 1;
        public string CuttingFileCode { get; set; }
        public string CuttingFileName { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public List<Guid> ProductionCommandIds { get; set; }
        public string MaterialCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }
        public int Quantity { get; set; }
        public decimal CuttingLength { get; set; }
        public decimal CuttingTime { get; set; }
        public int PlannedQuantity { get; set; }
        public NoteType Note { get; set; }
        public string Description { get; set; }
        public decimal RemainLength { get; set; }
        public decimal RemainWidth { get; set; }
        public decimal RemainThickness { get; set; }
    }

    public class UpdateCommandCuttingFileRequest
    {
        //public string CuttingFileCode { get; set; }
        //public string CuttingFileName { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public List<Guid> ProductionCommandIds { get; set; }
        public int Priority { get; set; }
        public string MaterialCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }
        public int Quantity { get; set; }
        public decimal CuttingLength { get; set; }
        public decimal CuttingTime { get; set; }
        public int PlannedQuantity { get; set; }
        public NoteType Note { get; set; }
        public string Description { get; set; }
        public decimal RemainLength { get; set; }
        public decimal RemainWidth { get; set; }
        public decimal RemainThickness { get; set; }
    }

    public class CommandCuttingFileListQuery
    {
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? ProductionCommandId { get; set; }
        public Guid? CuttingFileId { get; set; }
    }

    public class ImportCommandCuttingFileDto
    {
        public int RowIndex { get; set; }
        public string CuttingFileCode { get; set; }
        public string CuttingFileName { get; set; }
        public string CategoryCuttingFileCode { get; set; }
        public string ProductionCommandCodes { get; set; }
        public int Priority { get; set; }
        public string MaterialCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }
        public int Quantity { get; set; }
        public decimal CuttingLength { get; set; }
        public decimal CuttingTime { get; set; }
        public int Note { get; set; }
        public string Description { get; set; }
        public decimal RemainLength { get; set; }
        public decimal RemainWidth { get; set; }
        public decimal RemainThickness { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ImportCommandCuttingFileResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ImportCommandCuttingFileDto> Errors { get; set; }
        public List<CommandCuttingFileDto> Created { get; set; }
    }
}
