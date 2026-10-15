using CoKhiThaCo.Models.Enums;
using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Execute
{
    public class ExecuteDto
    {
        public Guid Id { get; set; }
        public Guid PlanningId { get; set; }
        public string CuttingFileCode { get; set; }
        public string CuttingFileName { get; set; }
        public string ProductionCommandCode { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public Guid MaterialFeedingPlanId { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public int ReceiveStatus { get; set; }
        public string ReceiveStatusName { get; set; }
        public DateTime? ExecuteStartTime { get; set; }
        public DateTime? ExecuteEndTime { get; set; }
        public bool IsOverdue { get; set; }
        public string Reason { get; set; }
        public int ActualQuantity { get; set; }
        public decimal ActualTime { get; set; }
        public int PlannedQuantity { get; set; }
        public string MaterialCode { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Thickness { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Quantity { get; set; }
        public int QuantityReceive { get; set; }
        public int QuantityNotConfirm { get; set; }
        public List<SubExecuteDto> SubExecutes { get; set; }
    }

    public class SubExecuteDto
    {
        public Guid Id { get; set; }
        public Guid ExecuteId { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ActualQuantity { get; set; }
        public int MaterialUsed { get; set; }
        public string Reason { get; set; }
        public ExecuteType Type { get; set; }
    }

    public class StartExecuteRequest
    {
        public Guid PlanningId { get; set; }
    }

    public class CompleteExecuteRequest
    {
        public int ActualQuantity { get; set; }
        public decimal ActualTime { get; set; }
        public string Reason { get; set; }
    }

    public class CancelExecuteRequest
    {
        public string Reason { get; set; }
    }

    public class ComfirmReceiveRequest
    {
        //public Guid MaterialFeedingPlanId { get; set; }
        public int QuantityConfirm { get; set; }
    }
    public class CompletedExecute 
    {
        public Guid ExecuteId { get; set; }
    }

    public class StartSubExecuteRequest
    {
        public Guid ExecuteId { get; set; }
        public ExecuteType Type { get; set; }
        public string Reason { get; set; }
    }
    public class ContitnuteSubExecuteRequest
    {
        public Guid ExecuteId { get; set; }
    }

    public class EndSubExecuteRequest
    {
        public Guid ExecuteId { get; set; }
        //public int ActualQuantity { get; set; }
        public int MaterialUsed { get; set; }
        public string Reason { get; set; }
    }

    public class ExecuteListQuery
    {
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? MachineId { get; set; }
    }

    public class SummaryStatusExecute
    {
        public int TotalCompleted { get; set; }
        public int TotalUnCompleted { get; set; }
        public int TotalActive { get; set; }
        public int TotalCompletedAPart { get; set; }
    }
}