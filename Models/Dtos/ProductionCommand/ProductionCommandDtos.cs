using System;

namespace CoKhiThaCo.Models.Dtos.ProductionCommand
{
    public class ProductionCommandDto
    {
        public Guid Id { get; set; }
        public string CommandCode { get; set; }
        public string OrderName { get; set; }
        public int CuttingFileCount { get; set; }
        public int TotalQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateProductionCommandRequest
    {
        public string CommandCode { get; set; }
        public string OrderName { get; set; }
    }

    public class UpdateProductionCommandRequest
    {
        public string CommandCode { get; set; }
        public string OrderName { get; set; }
    }

    public class ProductionCommandListQuery
    {
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ImportProductionCommandDto
    {
        public int RowIndex { get; set; }
        public string CommandCode { get; set; }
        public string OrderName { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ImportProductionCommandDto> Errors { get; set; }
        public List<ProductionCommandDto> Created { get; set; }
    }
}
