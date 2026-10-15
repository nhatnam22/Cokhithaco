using System;

namespace CoKhiThaCo.Models.Dtos.MaterialFeedingPlan
{
    public class MaterialFeedingPlanDto
    {
        public Guid Id { get; set; }
        public Guid PlanningId { get; set; }
        public string CommandCuttingFileCode { get; set; }
        public string CuttingFileName { get; set; }
        public string ProductionCommandCode { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public int Quantity { get; set; }
        public int QuantityActual { get; set; }
        public string MaterialCode { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Thickness { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? TimeAtCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateMaterialFeedingPlanRequest
    {
        public Guid PlanningId { get; set; }
        public int Quantity { get; set; }
        //public decimal? Length { get; set; }
        //public decimal? Width { get; set; }
        //public decimal? Thickness { get; set; }
        public bool IsDirector { get; set; }
    }

    public class UpdateMaterialFeedingPlanRequest
    {
        public int Quantity { get; set; }
        public int QuantityActual { get; set; }
        //public decimal? Length { get; set; }
        //public decimal? Width { get; set; }
        //public decimal? Thickness { get; set; }
        //public string Description { get; set; }
    }

    public class CompleteMaterialFeedingPlanRequest
    {
        public int QuantityActual { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Thickness { get; set; }
        public string Description { get; set; }
    }

    public class MaterialFeedingPlanListQuery
    {
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? PlanningId { get; set; }
        public int? Status { get; set; }
        public string Keyword { get; set; }
        public List<Guid> MachineIds { get; set; }
    }
    public class SummaryMaterialFeedingStatus
    {
        public int TotalPrepagation { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalCancel { get; set; }
    }

    public class SummaryTodayMaterialFeedingStatus
    {
        public int TotalNeedFeeding { get; set; }
        public int TotalFedEnough { get; set; }
        public int TotalNotFedEnough { get; set; }
        public int TotalOverdue { get; set; }
        public int TotalNotOverdue { get; set; }
    }
}