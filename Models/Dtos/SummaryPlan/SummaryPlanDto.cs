using CoKhiThaCo.Models.Dtos.CommandCuttingFile;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models.Dtos.SummaryPlan
{
    public class SummaryPlanDto
    {
        public Guid PlanningId { get; set; }
        public Guid CommandCuttingFileId { get; set; }
        public Guid CuttingFileId { get; set; }
        public string CuttingFileName { get; set; }
        public List<CommandProductionCommandDto> ProductionCommands { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public string CategoryCuttingFileName { get; set; }
        public string MaterialCode { get; set; }
        public decimal CuttingTime { get; set; }
        public int Quantity { get; set; }
        public int ReceiveQuantity { get; set; }
        public int QuantityNotConfirm { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public PlanningStatus statusCode { get; set; }
        public string StatusName { get; set; }
        public Guid? ExecuteId { get; set; }
        public ExecuteStatus? ExecuteStatus { get; set; }
        public string ExecuteStatusName { get; set; }
        public ReceiveMaterialFeedStatus? ReceiveStatus { get; set; }
        public string ReceiveStatusName { get; set; }
        public int TotalSubExecute { get; set; }
        public int TotalActualQuantity { get; set; }
        public int TotalMaterialUsed { get; set; }
        public bool IsOverdue { get; set; }
        public decimal ProgressPercent { get; set; }
    }

    public class SummaryRequest
    {
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? MachineId { get; set; }
        public int? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}