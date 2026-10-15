using System;

namespace CoKhiThaCo.Models.Dtos.CuttingFile
{
    public class CuttingFileDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        public string CategoryCuttingFileCode { get; set; }
        public string CategoryCuttingFileName { get; set; }
        public int CommandCount { get; set; }
    }

    public class CreateCuttingFileRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
    }

    public class UpdateCuttingFileRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
    }

    public class CuttingFileListQuery
    {
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? CategoryCuttingFileId { get; set; }
    }
}
