using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class MachineOperator : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public Data.ApplicationUser User { get; set; }

        public Guid MachineId { get; set; }
        [ForeignKey("MachineId")]
        public Machine Machine { get; set; }
    }
}
