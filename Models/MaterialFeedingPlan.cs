using CoKhiThaCo.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class MaterialFeedingPlan : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PlanningId { get; set; }
        [ForeignKey("PlanningId")]
        public Planning Planning { get; set; }

        public Guid? UserId { get; set; }
        [ForeignKey("UserId")]
        public Data.ApplicationUser User { get; set; }

        public MaterialFeeding Status { get; set; } = MaterialFeeding.Preparation;

        public int Quantity { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Thickness { get; set; }

        public int QuantityActual { get; set; }

        public DateTime? TimeAtCompleted { get; set; }
        public bool IsDirector { get; set; }
        public string Description { get; set; }
    }
}