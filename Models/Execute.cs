using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models
{
    public class Execute : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PlanningId { get; set; }
        [ForeignKey("PlanningId")]
        public Planning Planning { get; set; }

        public Guid? UserId { get; set; }
        [ForeignKey("UserId")]
        public Data.ApplicationUser User { get; set; }

        public ExecuteStatus Status { get; set; } = ExecuteStatus.UnCompleted;

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public bool IsOverdue { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }

        public int ActualQuantity { get; set; }
        public decimal ActualTime { get; set; }
        public ReceiveMaterialFeedStatus ReceiveStatus { get; set; }
    }
}
