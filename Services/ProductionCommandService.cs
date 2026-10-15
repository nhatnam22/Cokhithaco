using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.ProductionCommand;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace CoKhiThaCo.Services
{
    public interface IProductionCommandService
    {
        PagedResult<ProductionCommandDto> GetPaged(ProductionCommandListQuery query);
        ProductionCommandDto GetById(Guid id);
        ProductionCommandDto Create(CreateProductionCommandRequest request);
        ProductionCommandDto Update(Guid id, UpdateProductionCommandRequest request);
        bool Delete(Guid id);
        ImportResultDto ImportFromExcel(IFormFile file);
    }

    public class ProductionCommandService : IProductionCommandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductionCommandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<ProductionCommandDto> GetPaged(ProductionCommandListQuery query)
        {
            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();

            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "CreatedDate", Direction = "desc" });
            var isHasSeacrh = string.IsNullOrEmpty(query.Keyword);
            var items = repo.GetAllPaging(p => !p.IsDeleted && (isHasSeacrh || p.CommandCode.Contains(query.Keyword) || p.OrderName.Contains(query.Keyword)), out var total, query.Page, query.PageSize, sorts, filters);

            // [THAY ĐỔI] Đếm CuttingFile theo collection ProductionCommands thay vì FK đơn
            var dtos = items.Select(p => new ProductionCommandDto
            {
                Id = p.Id,
                CommandCode = p.CommandCode,
                OrderName = p.OrderName,
                CuttingFileCount = commandFileRepo.Count(c => c.ProductionCommands.Any(pc => pc.Id == p.Id) && !c.IsDeleted),
                TotalQuantity = commandFileRepo.GetAll(c => c.ProductionCommands.Any(pc => pc.Id == p.Id) && !c.IsDeleted).Sum(c => c.Quantity),
                CreatedDate = p.CreatedDate
            }).ToList();

            return new PagedResult<ProductionCommandDto> { Items = dtos, Total = total, Page = query.Page, PageSize = query.PageSize };
        }

        public ProductionCommandDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            var item = repo.GetSingle(p => p.Id == id && !p.IsDeleted);
            if (item == null) return null;

            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();

            // [THAY ĐỔI] Đếm CuttingFile theo collection ProductionCommands thay vì FK đơn
            return new ProductionCommandDto
            {
                Id = item.Id,
                CommandCode = item.CommandCode,
                OrderName = item.OrderName,
                CuttingFileCount = commandFileRepo.Count(c => c.ProductionCommands.Any(pc => pc.Id == id) && !c.IsDeleted),
                TotalQuantity = commandFileRepo.GetAll(c => c.ProductionCommands.Any(pc => pc.Id == id) && !c.IsDeleted).Sum(c => c.Quantity),
                CreatedDate = item.CreatedDate
            };
        }

        public ProductionCommandDto Create(CreateProductionCommandRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CommandCode) || string.IsNullOrWhiteSpace(request.OrderName))
                throw new Exception("Mã lệnh sản xuất và tên đơn hàng là bắt buộc");

            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            if (repo.Exists(p => p.CommandCode == request.CommandCode && !p.IsDeleted))
                throw new Exception("Mã lệnh sản xuất đã tồn tại");

            var item = new ProductionCommand
            {
                Id = Guid.NewGuid(),
                CommandCode = request.CommandCode,
                OrderName = request.OrderName,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(item);
            _unitOfWork.Complete();
            return GetById(item.Id);
        }

        public ProductionCommandDto Update(Guid id, UpdateProductionCommandRequest request)
        {
            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return null;

            if (repo.Exists(p => p.CommandCode == request.CommandCode && p.Id != id && !p.IsDeleted))
                throw new Exception("Mã lệnh sản xuất đã tồn tại");

            item.CommandCode = request.CommandCode;
            item.OrderName = request.OrderName;
            item.UpdatedDate = TimeHelper.Now;

            repo.Update(item);
            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return false;

            // [THAY ĐỔI] Kiểm tra xóa theo collection ProductionCommands thay vì FK đơn
            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            if (commandFileRepo.Exists(c => c.ProductionCommands.Any(pc => pc.Id == id) && !c.IsDeleted))
                throw new Exception("Không thể xóa lệnh sản xuất có chứa tệp cắt");

            item.IsDeleted = true;
            item.DeletedDate = TimeHelper.Now;
            repo.Update(item);
            _unitOfWork.Complete();
            return true;
        }

        public ImportResultDto ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Vui lòng chọn file Excel để import");

            var result = new ImportResultDto
            {
                Errors = new List<ImportProductionCommandDto>(),
                Created = new List<ProductionCommandDto>()
            };

            var commandCodeProp = typeof(ProductionCommand).GetProperty(nameof(ProductionCommand.CommandCode));
            var orderNameProp = typeof(ProductionCommand).GetProperty(nameof(ProductionCommand.OrderName));

            if (commandCodeProp.PropertyType != typeof(string))
                throw new Exception("CommandCode phải là kiểu string");
            if (orderNameProp.PropertyType != typeof(string))
                throw new Exception("OrderName phải là kiểu string");

            var rows = new List<ImportProductionCommandDto>();

            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        throw new Exception("File Excel không có sheet nào");

                    var headerA = worksheet.Cells[1, 1].Text?.Trim();
                    var headerB = worksheet.Cells[1, 2].Text?.Trim();

                    if (headerA != "LSX CHÍNH" || headerB != "TÊN ĐƠN HÀNG")
                        throw new Exception("Header không đúng định dạng. Cần: LSX CHÍNH, TÊN ĐƠN HÀNG");

                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    result.TotalRows = rowCount - 1;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var commandCode = worksheet.Cells[row, 1].Text?.Trim();
                        var orderName = worksheet.Cells[row, 2].Text?.Trim();

                        var importRow = new ImportProductionCommandDto
                        {
                            RowIndex = row,
                            CommandCode = commandCode,
                            OrderName = orderName,
                            IsValid = true
                        };

                        if (string.IsNullOrWhiteSpace(commandCode))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Mã lệnh không được để trống";
                        }
                        else if (string.IsNullOrWhiteSpace(orderName))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Tên đơn hàng không được để trống";
                        }

                        rows.Add(importRow);
                    }
                }
            }

            var fileDuplicates = rows
                .Where(r => r.IsValid && !string.IsNullOrWhiteSpace(r.CommandCode))
                .GroupBy(r => r.CommandCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (var row in rows.Where(r => r.IsValid))
            {
                if (fileDuplicates.Contains(row.CommandCode))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã lệnh \"{row.CommandCode}\" bị trùng trong file import";
                }
            }

            var validCommandCodes = rows
                .Where(r => r.IsValid)
                .Select(r => r.CommandCode)
                .Distinct()
                .ToList();

            var repo = _unitOfWork.GetRepository<ProductionCommand>();
            var existingCodes = repo.GetAll(p => validCommandCodes.Contains(p.CommandCode) && !p.IsDeleted)
                .Select(p => p.CommandCode)
                .ToHashSet();

            foreach (var row in rows.Where(r => r.IsValid))
            {
                if (existingCodes.Contains(row.CommandCode))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã lệnh \"{row.CommandCode}\" đã tồn tại trong database";
                }
            }

            var validRows = rows.Where(r => r.IsValid).ToList();
            var errorRows = rows.Where(r => !r.IsValid).ToList();

            foreach (var row in validRows)
            {
                var item = new ProductionCommand
                {
                    Id = Guid.NewGuid(),
                    CommandCode = row.CommandCode,
                    OrderName = row.OrderName,
                    CreatedDate = TimeHelper.Now
                };
                repo.Add(item);

                result.Created.Add(new ProductionCommandDto
                {
                    Id = item.Id,
                    CommandCode = item.CommandCode,
                    OrderName = item.OrderName,
                    CreatedDate = item.CreatedDate
                });
            }

            if (validRows.Any())
                _unitOfWork.Complete();

            result.SuccessCount = validRows.Count;
            result.ErrorCount = errorRows.Count;
            result.Errors = errorRows;

            return result;
        }
    }
}
