using System;

namespace CoKhiThaCo.Models.Dtos.Machine
{
    public class MachineCategoryCuttingFileDto
    {
        public Guid Id { get; set; }
        public Guid MachineId { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public string CategoryCuttingFileCode { get; set; }
        public string CategoryCuttingFileName { get; set; }
    }

    public class AssignCategoryCuttingFilesRequest
    {
        public Guid MachineId { get; set; }
        public List<Guid> CategoryCuttingFileIds { get; set; } = new();
    }
}
