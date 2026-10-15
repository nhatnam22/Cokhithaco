using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models
{
    public class Planning : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CommandCuttingFileId { get; set; }
        [ForeignKey("CommandCuttingFileId")]
        public CommandCuttingFile CommandCuttingFile { get; set; }

        public Guid? MachineId { get; set; }
        [ForeignKey("MachineId")]
        public Machine Machine { get; set; }

        //public Guid? WorkShiftId { get; set; }
        //[ForeignKey("WorkShiftId")]
        //public WorkShift WorkShift { get; set; }

        public int OrderIndex { get; set; }
        public PlanningStatus Status { get; set; } = PlanningStatus.Pending;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public ICollection<PlanningSegment> Segments { get; set; }
    }
}
