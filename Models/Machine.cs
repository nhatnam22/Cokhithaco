using System.ComponentModel.DataAnnotations;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Models
{
    public class Machine : Auditable
    {
        [Key]
        public Guid MachineId { get; set; }

        [Required, MaxLength(50)]
        public string MachineCode { get; set; }

        [Required, MaxLength(200)]
        public string MachineName { get; set; }

        public MachineStatus Status { get; set; } = MachineStatus.Active;

        public decimal PowerKw { get; set; }

        public bool IsReserveMachine { get; set; } = false;

        public decimal? MinCutLengthMm { get; set; }
        public decimal? MaxCutLengthMm { get; set; }
        public decimal? MinCutThicknessMm { get; set; }
        public decimal? MaxCutThicknessMm { get; set; }
        public int PerformancePercent { get; set; }
    }
}
