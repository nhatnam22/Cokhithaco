using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class WorkShiftMachine : Auditable
    {
        [Key]
        public Guid Id { get; set; }
        public Guid WorkShiftId { get; set; }
        public Guid MachineId { get; set; }
        [ForeignKey("WorkShiftId")]
        public WorkShift WorkShift { get; set; }
        [ForeignKey("MachineId")]
        public Machine Machine { get; set; }
    }
}
