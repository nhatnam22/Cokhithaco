using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class ShiftBreak : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? WorkShiftId { get; set; }
        [ForeignKey("WorkShiftId")]
        public WorkShift WorkShift { get; set; }

        [Required, MaxLength(200)]
        public string? BreakName { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public decimal DurationMinutes { get; set; }
    }
}