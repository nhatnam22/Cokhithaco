using System;

namespace CoKhiThaCo.Models.Dtos.CategoryCuttingFile
{
    public class CategoryCuttingFileDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CuttingFileCount { get; set; }
        public int MachineCount { get; set; }
        public bool IsMaterial { get; set; }
    }

    public class CreateCategoryCuttingFileRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsMaterial { get; set; }
    }

    public class UpdateCategoryCuttingFileRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsMaterial { get; set; }
    }

    public class CategoryCuttingFileListQuery
    {
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
