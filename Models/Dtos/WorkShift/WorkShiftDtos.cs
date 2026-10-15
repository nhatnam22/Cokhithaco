using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models.Dtos.WorkShift
{
    public class WorkShiftDto
    {
        public Guid Id { get; set; }
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal StandardTimeMinutes { get; set; }
        public List<ShiftBreakDto> Breaks { get; set; }
    }

    public class CreateWorkShiftRequest
    {
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        //public decimal StandardTimeMinutes { get; set; }
        public List<CreateShiftBreakRequest> ShiftBreaks { get; set; }
    }

    public class UpdateWorkShiftRequest
    {
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
        public TimeSpan StartTime { get; set; }
        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
        public TimeSpan EndTime { get; set; }
        //public decimal StandardTimeMinutes { get; set; }
        public List<UpdateShiftBreakRequest> ShiftBreaks { get; set; }
    }

    public class WorkShiftListQuery
    {
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ShiftBreakDto
    {
        public Guid Id { get; set; }
        public Guid? WorkShiftId { get; set; }
        public string BreakName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationMinutes { get; set; }
    }

    public class CreateShiftBreakRequest
    {
        public Guid WorkShiftId { get; set; }
        public string BreakName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateShiftBreakRequest
    {
        public Guid? Id { get; set; }
        public string BreakName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}