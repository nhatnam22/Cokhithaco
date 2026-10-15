using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class PlanningSegment
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PlanningId { get; set; }
        [ForeignKey("PlanningId")]
        public Planning Planning { get; set; }

        public int OrderIndex { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}