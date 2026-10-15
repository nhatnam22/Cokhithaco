using CoKhiThaCo.Models.Dtos.WorkShift;
using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Machine
{
    public class MachineDto
    {
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public decimal PowerKw { get; set; }
        public bool IsReserveMachine { get; set; }
        public decimal? MinCutLengthMm { get; set; }
        public decimal? MaxCutLengthMm { get; set; }
        public decimal? MinCutThicknessMm { get; set; }
        public decimal? MaxCutThicknessMm { get; set; }
        public int PerformancePercent { get; set; }
        public List<MachineOperatorDto> Operators { get; set; }
        public List<CategoryCuttingFileDto> CategoryCuttingFiles { get; set; }
        public List<WorkShiftDto> WorkShifts { get; set; }
    }

    public class CreateMachineRequest
    {
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public decimal PowerKw { get; set; }
        public bool IsReserveMachine { get; set; } = false;
        public decimal? MinCutLengthMm { get; set; }
        public decimal? MaxCutLengthMm { get; set; }
        public decimal? MinCutThicknessMm { get; set; }
        public decimal? MaxCutThicknessMm { get; set; }
        public int PerformancePercent { get; set; }
        public List<Guid> WorkShiftIds { get; set; }
        public List<Guid> CategoryCuttingFileIds { get; set; }
    }

    public class UpdateMachineRequest
    {
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public int Status { get; set; }
        public decimal PowerKw { get; set; }
        public bool IsReserveMachine { get; set; } = false;
        public decimal? MinCutLengthMm { get; set; }
        public decimal? MaxCutLengthMm { get; set; }
        public decimal? MinCutThicknessMm { get; set; }
        public decimal? MaxCutThicknessMm { get; set; }
        public int PerformancePercent { get; set; }
        public List<Guid> WorkShiftIds { get; set; }
        public List<Guid> CategoryCuttingFileIds { get; set; }
    }

    public class MachineListQuery
    {
        public bool IsChooseMachinePlan { get; set; }
        public string CommandCuttingFileSearch { get; set; }
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class MachineOperatorDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; }
    }

    public class AssignMachinesToUserRequest
    {
        public Guid UserId { get; set; }
        public List<Guid> MachineIds { get; set; } = new();
    }

    public class AssignWorkShiftsToMachineRequest
    {
        public Guid MachineId { get; set; }
        public List<Guid> WorkShiftIds { get; set; } = new();
    }

    public class AssignCategoriesToMachineRequest
    {
        public Guid MachineId { get; set; }
        public List<Guid> CategoryCuttingFileIds { get; set; } = new();
    }

    public class CategoryCuttingFileDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}