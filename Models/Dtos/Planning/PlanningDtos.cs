using CoKhiThaCo.Models.Dtos.ProductionCommand;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Planning
{
    public class PlanningSegmentDto
    {
        public Guid Id { get; set; }
        public Guid PlanningId { get; set; }
        public int OrderIndex { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class PlanningDto
    {
        public Guid Id { get; set; }
        public Guid CommandCuttingFileId { get; set; }
        public string FileCode { get; set; }
        public string FileName { get; set; }
        public List<ProductionCommandDto> ProductionCommandCodes { get; set; }
        public string OrderName { get; set; }
        public Guid? MachineId { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public int OrderIndex { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Quantity { get; set; }
        public decimal CuttingTime { get; set; }
        public decimal Thickness { get; set; }
        public string CategoryCuttingFileCode { get; set; }
        public List<PlanningSegmentDto> Segments { get; set; }
    }

    public class CreatePlanningRequest
    {
        public Guid CommandCuttingFileId { get; set; }
        public Guid MachineId { get; set; }
        public int OrderIndex { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class CreateAutoPlanningRequest
    {
        public Guid CommandCuttingFileId { get; set; }
    }
    public class SchedulePlanningRequest
    {
        public Guid MachineId { get; set; }
        public DateTime Date { get; set; }
    }

    public class UpdatePlanningRequest
    {
        public Guid MachineId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class PlanningListQuery
    {
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? MachineId { get; set; }
        public string Keyword { get; set; }
    }

    public class SummaryStatusPlan
    {
        public int TotalPendingStatus { get; set; }
        public int TotalProcessStatus { get; set; }
        public int TotalCompletedStatus { get; set; }
        public int TotalCancelStatus { get; set; }
        public int TotalDone { get; set; }
    }
    public class PlanningGroupMachine
    {
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public int Status { get; set; }
        public decimal PowerKw { get; set; }
        public bool IsReserveMachine { get; set; }
        public decimal TimeRemain { get; set; }
        public List<PlanningDto> Plans { get; set; }
    }

    public class BatchUpdateScheduleItem
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class BatchUpdateScheduleRequest
    {
        public List<BatchUpdateScheduleItem> Items { get; set; }
    }

    public class BatchScheduleRequest
    {
        public List<Guid> PlanningIds { get; set; }
        public Guid MachineId { get; set; }
        public DateTime Date { get; set; }
    }

    public class SwapPlanningRequest
    {
        public Guid FirstPlanningId { get; set; }
        public Guid SecondPlanningId { get; set; }
    }
}