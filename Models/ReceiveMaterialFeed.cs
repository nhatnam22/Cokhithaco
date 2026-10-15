using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class ReceiveMaterialFeed : Auditable
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ExecuteId { get; set; }
        [ForeignKey("ExecuteId")]
        public Execute Execute { get; set; }
        public int QuantityConfirmReceived { get; set; }
        public DateTime ReceivedTime { get; set; }
    }
}
