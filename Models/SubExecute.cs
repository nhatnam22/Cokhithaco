using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models
{
    public class SubExecute : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ExecuteId { get; set; }
        [ForeignKey("ExecuteId")]
        public Execute Execute { get; set; }

        public Guid? UserId { get; set; }
        [ForeignKey("UserId")]
        public Data.ApplicationUser User { get; set; }

        public ExecuteStatus Status { get; set; } = ExecuteStatus.UnCompleted;

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int ActualQuantity { get; set; }
        public int MaterialUsed { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
        public ExecuteType Type { get; set; }
    }
}