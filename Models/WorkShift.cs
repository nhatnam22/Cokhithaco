using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class WorkShift : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string ShiftCode { get; set; }

        [Required, MaxLength(200)]
        public string ShiftName { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal StandardTimeMinutes { get; set; }
    }
}
