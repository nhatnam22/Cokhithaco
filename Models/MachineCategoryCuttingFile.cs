using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class MachineCategoryCuttingFile : Auditable
    {
        public Guid Id { get; set; }
        public Guid MachineId { get; set; }
        [ForeignKey("MachineId")]
        public Machine Machine { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        [ForeignKey("CategoryCuttingFileId")]
        public CategoryCuttingFile CategoryCuttingFile { get; set; }
    }
}
