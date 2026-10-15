using Azure.Core;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CommandCuttingFile;
using CoKhiThaCo.Models.Dtos.Planning;
using CoKhiThaCo.Models.Enums;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CoKhiThaCo.Services
{
    public interface ICommandCuttingFileService
    {
        PagedResult<CommandCuttingFileDto> GetPaged(CommandCuttingFileListQuery query);
        CommandCuttingFileDto GetById(Guid id);
        CommandCuttingFileDto Create(CreateCommandCuttingFileRequest request);
        CommandCuttingFileDto Update(Guid id, UpdateCommandCuttingFileRequest request);
        bool Delete(Guid id);
        void UpdateStatus(Guid id, ProductionOrderStatus status);
        void UpdatePlannedQuantity(Guid id, int delta);
        void UpdateCompletedQuantity(Guid id, int delta);
        ImportCommandCuttingFileResultDto ImportFromExcel(IFormFile file);
    }

    public class CommandCuttingFileService : ICommandCuttingFileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPlanningService _planningService;

        public CommandCuttingFileService(IUnitOfWork unitOfWork, IPlanningService planningService)
        {
            _unitOfWork = unitOfWork;
            _planningService = planningService;
        }

        #region Helper Methods

        // [THAY ĐỔI] Include thêm "ProductionCommands" (collection thay vì nav đơn)
        private CommandCuttingFile GetEntity(Guid id, string[] includes = null)
        {
            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            return repo.GetSingle(c => c.Id == id && !c.IsDeleted, includes: includes);
        }

        private void ValidateCategoryCuttingFile(Guid categoryCuttingFileId)
        {
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            if (!categoryRepo.Exists(c => c.Id == categoryCuttingFileId && !c.IsDeleted))
                throw new Exception("Loại file cắt không tồn tại");
        }

        // [THAY ĐỔI] ValidateProductionCommands: validate tồn tại và trả về entities
        private List<ProductionCommand> ValidateProductionCommands(List<Guid> productionCommandIds)
        {
            if (productionCommandIds == null || !productionCommandIds.Any())
                throw new Exception("Phải chọn ít nhất một lệnh sản xuất");

            var commandRepo = _unitOfWork.GetRepository<ProductionCommand>();
            var commands = commandRepo.GetAll(c => productionCommandIds.Contains(c.Id) && !c.IsDeleted).ToList();
            if (commands.Count != productionCommandIds.Count)
                throw new Exception("Một hoặc nhiều lệnh sản xuất không tồn tại");

            // [THÊM] Detach ProductionCommand khỏi change tracker
            // để EF Core không INSERT lại khi gán vào navigation property
            foreach (var pc in commands)
            {
                _unitOfWork.Context.Entry(pc).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
            }

            return commands;
        }

        private CuttingFile CreateCuttingFile(CreateCommandCuttingFileRequest request)
        {
            var cuttingFileRepo = _unitOfWork.GetRepository<CuttingFile>();

            if (cuttingFileRepo.Exists(cf => cf.Code == request.CuttingFileCode && !cf.IsDeleted))
                throw new Exception("Mã file cắt đã tồn tại");

            var cuttingFile = new CuttingFile
            {
                Id = Guid.NewGuid(),
                Code = request.CuttingFileCode,
                Name = string.IsNullOrWhiteSpace(request.CuttingFileName) ? request.CuttingFileCode : request.CuttingFileName,
                CategoryCuttingFileId = request.CategoryCuttingFileId,
                CreatedDate = TimeHelper.Now
            };

            cuttingFileRepo.Add(cuttingFile);
            return cuttingFile;
        }

        // [THAY ĐỔI] ValidateNoDuplicate: 1 CuttingFile = 1 CommandCuttingFile
        // Không cần check theo ProductionCommandId nữa, chỉ check xem CuttingFile đã có CCF chưa
        private void ValidateNoDuplicate(CuttingFile cuttingFile)
        {
            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            if (repo.Exists(c => c.CuttingFileId == cuttingFile.Id && !c.IsDeleted))
                throw new Exception("File cắt này đã tồn tại trong hệ thống");
        }

        #endregion

        #region CRUD

        // [THAY ĐỔI] GetPaged: dùng ProductionCommands collection thay vì ProductionCommandId
        public PagedResult<CommandCuttingFileDto> GetPaged(CommandCuttingFileListQuery query)
        {
            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "CreatedDate", Direction = "desc" });

            var isHasKeyword = string.IsNullOrEmpty(query.Keyword);
            // [THAY ĐỔI] Query keyword: dùng ProductionCommands.Any() thay vì ProductionCommand.CommandCode
            Expression<Func<CommandCuttingFile, bool>> where = c => !c.IsDeleted
            && (isHasKeyword
                || c.ProductionCommands.Any(pc => pc.CommandCode.Contains(query.Keyword) || pc.OrderName.Contains(query.Keyword))
                || c.CuttingFile.Code.Contains(query.Keyword));

            // [THAY ĐỔI] Filter theo ProductionCommandId: dùng Any() trên collection
            if (query.ProductionCommandId.HasValue)
            {
                var cmdId = query.ProductionCommandId.Value;
                where = c => !c.IsDeleted && c.ProductionCommands.Any(pc => pc.Id == cmdId);
            }
            else if (query.CuttingFileId.HasValue)
            {
                var cfId = query.CuttingFileId.Value;
                where = c => !c.IsDeleted && c.CuttingFileId == cfId;
            }

            // [THAY ĐỔI] Include: "ProductionCommands" (collection) thay vì "ProductionCommand" (nav đơn)
            var items = repo.GetAllPaging(where, out var total, query.Page, query.PageSize, sorts, filters,
                includes: new[] { "CuttingFile", "ProductionCommands" });

            var dtos = items.Select(c => MapToDto(c, c.CuttingFile, c.ProductionCommands?.ToList() ?? new List<ProductionCommand>())).ToList();

            return new PagedResult<CommandCuttingFileDto>
            {
                Items = dtos,
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // [THAY ĐỔI] GetById: đơn giản hóa, lấy trực tiếp ProductionCommands từ entity
        public CommandCuttingFileDto GetById(Guid id)
        {
            var item = GetEntity(id, includes: new[] { "CuttingFile.CategoryCuttingFile", "ProductionCommands" });
            if (item == null) return null;

            return MapToDto(item, item.CuttingFile, item.ProductionCommands?.ToList() ?? new List<ProductionCommand>());
        }

        // [THAY ĐỔI] Create: tạo 1 CommandCuttingFile với nhiều ProductionCommand
        // Trước đây: tạo N CommandCuttingFile (mỗi ProductionCommandId 1 dòng)
        // Bây giờ: tạo 1 CommandCuttingFile, gán N ProductionCommand vào collection
        public CommandCuttingFileDto Create(CreateCommandCuttingFileRequest request)
        {
            if (request.Quantity <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            ValidateCategoryCuttingFile(request.CategoryCuttingFileId);
            var productionCommands = ValidateProductionCommands(request.ProductionCommandIds);

            var cuttingFile = CreateCuttingFile(request);
            ValidateNoDuplicate(cuttingFile);

            // [THAY ĐỔI] Tạo 1 CommandCuttingFile duy nhất với N ProductionCommand
            var item = new CommandCuttingFile
            {
                Id = Guid.NewGuid(),
                CuttingFileId = cuttingFile.Id,
                CategoryCuttingFileId = request.CategoryCuttingFileId,
                ProductionCommands = productionCommands, // Entities đã detach, EF Core sẽ chỉ tạo join table
                Priority = (Priority)request.Priority,
                MaterialCode = request.MaterialCode,
                Length = request.Length,
                Width = request.Width,
                Thickness = request.Thickness,
                Quantity = request.Quantity,
                CuttingLength = request.CuttingLength,
                CuttingTime = request.CuttingTime,
                Status = ProductionOrderStatus.Created,
                PlannedQuantity = request.PlannedQuantity,
                Note = request.Note,
                Description = request.Description,
                CreatedDate = TimeHelper.Now,
                RemainLength = request.RemainLength,
                RemainThickness = request.RemainThickness,
                RemainWidth = request.RemainWidth
            };

            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            repo.Add(item);
            _unitOfWork.Complete();

            // [THAY ĐỔI] Chỉ tạo 1 Planning (vì chỉ có 1 CommandCuttingFile)
            _planningService.CreateAuto(new CreateAutoPlanningRequest
            {
                CommandCuttingFileId = item.Id
            });

            return MapToDto(item, cuttingFile, productionCommands);
        }

        // [THAY ĐỔI] Update: giữ nguyên logic, chỉ cần thêm cập nhật ProductionCommands nếu cần
        public CommandCuttingFileDto Update(Guid id, UpdateCommandCuttingFileRequest request)
        {
            // 1. Lấy dữ liệu và track toàn bộ thay đổi
            var item = GetEntity(id, includes: new[] { "ProductionCommands" });
            if (item == null) return null;

            if (item.Status != ProductionOrderStatus.Created)
                throw new Exception("Chỉ các mục có trạng thái Created mới có thể được cập nhật");

            // 2. Gán các trường dữ liệu cơ bản
            item.CategoryCuttingFileId = request.CategoryCuttingFileId;
            item.Priority = (Priority)request.Priority;
            item.MaterialCode = request.MaterialCode;
            item.Length = request.Length;
            item.Width = request.Width;
            item.Thickness = request.Thickness;
            item.Quantity = request.Quantity;
            item.CuttingLength = request.CuttingLength;
            item.CuttingTime = request.CuttingTime;
            item.UpdatedDate = TimeHelper.Now;
            item.PlannedQuantity = request.PlannedQuantity;
            item.Note = request.Note;
            item.Description = request.Description;
            item.RemainLength = request.RemainLength;
            item.RemainThickness = request.RemainThickness;
            item.RemainWidth = request.RemainWidth;

            // 3. Xử lý logic cập nhật quan hệ (Bảng trung gian)
            if (request.ProductionCommandIds != null)
            {
                var currentCommandIds = item.ProductionCommands.Select(x => x.Id).ToList();

                // Tìm ID cần thêm mới
                var idsToAdd = request.ProductionCommandIds.Except(currentCommandIds).ToList();

                // Tìm Object cần xóa bỏ
                var commandsToRemove = item.ProductionCommands
                                           .Where(x => !request.ProductionCommandIds.Contains(x.Id))
                                           .ToList();

                // Xóa liên kết (EF tự DELETE ở bảng join)
                foreach (var cmd in commandsToRemove)
                {
                    item.ProductionCommands.Remove(cmd);
                }

                // Thêm liên kết mới (EF tự INSERT ở bảng join)
                if (idsToAdd.Any())
                {
                    var commandRepo = _unitOfWork.GetRepository<ProductionCommand>();
                    var newCommands = commandRepo.GetAll(c => idsToAdd.Contains(c.Id)).ToList();

                    foreach (var cmd in newCommands)
                    {
                        item.ProductionCommands.Add(cmd);
                    }
                }
            }

            // 4. LƯU THAY ĐỔI
            // Bỏ repo.Update(item) vì object đã được track sẵn.
            // Gọi Complete() sẽ kích hoạt việc tính toán sự khác biệt (Diff) và sinh ra SQL tự động.
            _unitOfWork.Complete();

            return GetById(id);
        }

        // [KHÔNG ĐỔI] Delete: giữ nguyên logic
        public bool Delete(Guid id)
        {
            var item = GetEntity(id);
            if (item == null) return false;

            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var plannings = planningRepo.GetAll(p => p.CommandCuttingFileId == id && !p.IsDeleted).ToList();
            foreach (var planning in plannings)
            {
                if (planning.Status == PlanningStatus.Pending)
                {
                    planning.Status = PlanningStatus.Cancelled;
                    planning.UpdatedDate = TimeHelper.Now;
                }
                else
                {
                    return false;
                }
                planning.IsDeleted = true;
                planning.DeletedDate = TimeHelper.Now;
                planningRepo.Update(planning);
            }

            item.IsDeleted = true;
            item.DeletedDate = TimeHelper.Now;

            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            repo.Update(item);
            _unitOfWork.Complete();
            return true;
        }

        #endregion

        #region Status & Quantity Updates

        // [KHÔNG ĐỔI] Các phương thức status/quantity không thay đổi
        public void UpdateStatus(Guid id, ProductionOrderStatus status)
        {
            var item = GetEntity(id);
            if (item == null) throw new Exception("Lệnh file cắt không tồn tại");

            item.Status = status;
            item.UpdatedDate = TimeHelper.Now;

            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            repo.Update(item);
            _unitOfWork.Complete();
        }

        public void UpdatePlannedQuantity(Guid id, int delta)
        {
            var item = GetEntity(id);
            if (item == null) throw new Exception("Lệnh file cắt không tồn tại");

            item.PlannedQuantity += delta;
            item.UpdatedDate = TimeHelper.Now;

            if (item.Status == ProductionOrderStatus.Created && item.PlannedQuantity > 0)
                item.Status = ProductionOrderStatus.Planned;

            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            repo.Update(item);
            _unitOfWork.Complete();
        }

        public void UpdateCompletedQuantity(Guid id, int delta)
        {
            var item = GetEntity(id);
            if (item == null) throw new Exception("Lệnh file cắt không tồn tại");

            item.CompletedQuantity += delta;
            item.UpdatedDate = TimeHelper.Now;

            if (item.CompletedQuantity >= item.Quantity)
                item.Status = ProductionOrderStatus.Completed;

            var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
            repo.Update(item);
            _unitOfWork.Complete();
        }

        #endregion

        #region Import

        public ImportCommandCuttingFileResultDto ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Vui lòng chọn file Excel để import");

            var result = new ImportCommandCuttingFileResultDto
            {
                Errors = new List<ImportCommandCuttingFileDto>(),
                Created = new List<CommandCuttingFileDto>()
            };

            var rows = new List<ImportCommandCuttingFileDto>();

            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        throw new Exception("File Excel không có sheet nào");

                    // Validate header: MA_FILE_CAT, TEN_FILE_CAT, LOAI_FILE_CAT, MA_LENH_SX, UU_TIEN, MA_VAT_LIEU, DAI, RONG, DAY, SO_LUONG, DAI_CAT, THOI_GIAN_CAT, GHI_CHU, MO_TA, DAI_CON_LAI, RONG_CON_LAI, DAY_CON_LAI
                    var expectedHeaders = new[] { "MA_FILE_CAT", "TEN_FILE_CAT", "LOAI_FILE_CAT", "MA_LENH_SX", "UU_TIEN", "MA_VAT_LIEU", "DAI", "RONG", "DAY", "SO_LUONG", "DAI_CAT", "THOI_GIAN_CAT", "GHI_CHU", "MO_TA", "DAI_CON_LAI", "RONG_CON_LAI", "DAY_CON_LAI" };
                    for (int col = 1; col <= expectedHeaders.Length; col++)
                    {
                        var header = worksheet.Cells[1, col].Text?.Trim();
                        if (header != expectedHeaders[col - 1])
                            throw new Exception($"Header cột {col} không đúng. Cần: {expectedHeaders[col - 1]}");
                    }

                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    result.TotalRows = rowCount - 1;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var importRow = new ImportCommandCuttingFileDto
                        {
                            RowIndex = row,
                            CuttingFileCode = worksheet.Cells[row, 1].Text?.Trim(),
                            CuttingFileName = worksheet.Cells[row, 2].Text?.Trim(),
                            CategoryCuttingFileCode = worksheet.Cells[row, 3].Text?.Trim(),
                            ProductionCommandCodes = worksheet.Cells[row, 4].Text?.Trim(),
                            Priority = int.TryParse(worksheet.Cells[row, 5].Text?.Trim(), out var pri) ? pri : 2,
                            MaterialCode = worksheet.Cells[row, 6].Text?.Trim(),
                            Length = decimal.TryParse(worksheet.Cells[row, 7].Text?.Trim(), out var len) ? len : 0,
                            Width = decimal.TryParse(worksheet.Cells[row, 8].Text?.Trim(), out var wid) ? wid : 0,
                            Thickness = decimal.TryParse(worksheet.Cells[row, 9].Text?.Trim(), out var thi) ? thi : 0,
                            Quantity = int.TryParse(worksheet.Cells[row, 10].Text?.Trim(), out var qty) ? qty : 0,
                            CuttingLength = decimal.TryParse(worksheet.Cells[row, 11].Text?.Trim(), out var cutLen) ? cutLen : 0,
                            CuttingTime = decimal.TryParse(worksheet.Cells[row, 12].Text?.Trim(), out var cutTime) ? cutTime : 0,
                            Note = int.TryParse(worksheet.Cells[row, 13].Text?.Trim(), out var note) ? note : 0,
                            Description = worksheet.Cells[row, 14].Text?.Trim(),
                            RemainLength = decimal.TryParse(worksheet.Cells[row, 15].Text?.Trim(), out var remLen) ? remLen : 0,
                            RemainWidth = decimal.TryParse(worksheet.Cells[row, 16].Text?.Trim(), out var remWid) ? remWid : 0,
                            RemainThickness = decimal.TryParse(worksheet.Cells[row, 17].Text?.Trim(), out var remThi) ? remThi : 0,
                            IsValid = true
                        };

                        // Validate MA_FILE_CAT
                        if (string.IsNullOrWhiteSpace(importRow.CuttingFileCode))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Mã file cắt không được để trống";
                        }
                        // Validate TEN_FILE_CAT
                        else if (string.IsNullOrWhiteSpace(importRow.CuttingFileName))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Tên file cắt không được để trống";
                        }
                        // Validate LOAI_FILE_CAT
                        else if (string.IsNullOrWhiteSpace(importRow.CategoryCuttingFileCode))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Mã loại file cắt không được để trống";
                        }
                        // Validate MA_LENH_SX
                        else if (string.IsNullOrWhiteSpace(importRow.ProductionCommandCodes))
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Mã lệnh sản xuất không được để trống";
                        }
                        // Validate SO_LUONG
                        else if (importRow.Quantity <= 0)
                        {
                            importRow.IsValid = false;
                            importRow.ErrorMessage = "Số lượng phải lớn hơn 0";
                        }

                        rows.Add(importRow);
                    }
                }
            }

            // Kiểm tra trùng MA_FILE_CAT trong file
            var fileDuplicates = rows
                .Where(r => r.IsValid && !string.IsNullOrWhiteSpace(r.CuttingFileCode))
                .GroupBy(r => r.CuttingFileCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (var row in rows.Where(r => r.IsValid))
            {
                if (fileDuplicates.Contains(row.CuttingFileCode))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã file cắt \"{row.CuttingFileCode}\" bị trùng trong file import";
                }
            }

            // Validate MA_FILE_CAT là Guid hợp lệ và tồn tại trong DB
            var cuttingFileRepo = _unitOfWork.GetRepository<CuttingFile>();
            var validCuttingFileGuids = new List<Guid>();
            foreach (var row in rows.Where(r => r.IsValid))
            {
                if (!Guid.TryParse(row.CuttingFileCode, out var cfGuid))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã file cắt \"{row.CuttingFileCode}\" không hợp lệ (không phải Guid)";
                }
                else
                {
                    validCuttingFileGuids.Add(cfGuid);
                }
            }

            var existingCuttingFileIds = cuttingFileRepo
                .GetAll(cf => validCuttingFileGuids.Contains(cf.Id) && !cf.IsDeleted)
                .Select(cf => cf.Id)
                .ToHashSet();

            foreach (var row in rows.Where(r => r.IsValid))
            {
                var cfId = Guid.Parse(row.CuttingFileCode);
                if (!existingCuttingFileIds.Contains(cfId))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã file cắt \"{row.CuttingFileCode}\" không tồn tại trong database";
                }
            }

            // Validate LOAI_FILE_CAT là Guid hợp lệ và tồn tại trong DB
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var validCategoryGuids = new List<Guid>();
            foreach (var row in rows.Where(r => r.IsValid))
            {
                if (!Guid.TryParse(row.CategoryCuttingFileCode, out var catGuid))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã loại file cắt \"{row.CategoryCuttingFileCode}\" không hợp lệ (không phải Guid)";
                }
                else
                {
                    validCategoryGuids.Add(catGuid);
                }
            }

            var existingCategories = categoryRepo
                .GetAll(c => validCategoryGuids.Contains(c.Id) && !c.IsDeleted)
                .ToDictionary(c => c.Id, c => c);

            foreach (var row in rows.Where(r => r.IsValid))
            {
                var catId = Guid.Parse(row.CategoryCuttingFileCode);
                if (!existingCategories.ContainsKey(catId))
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã loại file cắt \"{row.CategoryCuttingFileCode}\" không tồn tại trong database";
                }
            }

            // Validate MA_LENH_SX là Guid hợp lệ và tồn tại trong DB (phân tách bằng ";")
            var allProductionCommandGuids = new List<Guid>();
            foreach (var row in rows.Where(r => r.IsValid))
            {
                var codes = row.ProductionCommandCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
                foreach (var code in codes)
                {
                    if (!Guid.TryParse(code, out var pcGuid))
                    {
                        row.IsValid = false;
                        row.ErrorMessage = $"Mã lệnh sản xuất \"{code}\" không hợp lệ (không phải Guid)";
                        break;
                    }
                    allProductionCommandGuids.Add(pcGuid);
                }
            }

            var productionCommandRepo = _unitOfWork.GetRepository<ProductionCommand>();
            var existingProductionCommands = productionCommandRepo
                .GetAll(pc => allProductionCommandGuids.Distinct().Contains(pc.Id) && !pc.IsDeleted)
                .ToDictionary(pc => pc.Id, pc => pc);

            foreach (var row in rows.Where(r => r.IsValid))
            {
                var codes = row.ProductionCommandCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
                var missingCodes = codes.Where(c => !existingProductionCommands.ContainsKey(Guid.Parse(c))).ToList();
                if (missingCodes.Any())
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"Mã lệnh sản xuất \"{string.Join(", ", missingCodes)}\" không tồn tại trong database";
                }
            }

            var validRows = rows.Where(r => r.IsValid).ToList();
            var errorRows = rows.Where(r => !r.IsValid).ToList();

            // Tạo dữ liệu
            foreach (var row in validRows)
            {
                var cuttingFileId = Guid.Parse(row.CuttingFileCode);
                var cuttingFile = cuttingFileRepo.GetSingle(x => x.Id == cuttingFileId && !x.IsDeleted);
                if (cuttingFile == null)
                {
                    row.IsValid = false;
                    row.ErrorMessage = $"File cắt \"{row.CuttingFileCode}\" không tồn tại";
                    continue;
                }

                var catId = Guid.Parse(row.CategoryCuttingFileCode);
                var category = existingCategories[catId];

                var pcCodes = row.ProductionCommandCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
                var pcIds = pcCodes.Select(c => Guid.Parse(c)).ToList();
                var productionCommands = existingProductionCommands.Where(kv => pcIds.Contains(kv.Key)).Select(kv => kv.Value).ToList();

                // [THÊM] Detach ProductionCommand khỏi change tracker
                foreach (var pc in productionCommands)
                {
                    _unitOfWork.Context.Entry(pc).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

                var item = new CommandCuttingFile
                {
                    Id = Guid.NewGuid(),
                    CuttingFileId = cuttingFileId,
                    CategoryCuttingFileId = catId,
                    ProductionCommands = productionCommands,
                    Priority = (Priority)row.Priority,
                    MaterialCode = row.MaterialCode,
                    Length = row.Length,
                    Width = row.Width,
                    Thickness = row.Thickness,
                    Quantity = row.Quantity,
                    CuttingLength = row.CuttingLength,
                    CuttingTime = row.CuttingTime,
                    Status = ProductionOrderStatus.Created,
                    PlannedQuantity = 0,
                    Note = (NoteType)row.Note,
                    Description = row.Description,
                    CreatedDate = TimeHelper.Now,
                    RemainLength = row.RemainLength,
                    RemainWidth = row.RemainWidth,
                    RemainThickness = row.RemainThickness
                };

                var repo = _unitOfWork.GetRepository<CommandCuttingFile>();
                repo.Add(item);
                _unitOfWork.Complete();

                _planningService.CreateAuto(new CreateAutoPlanningRequest
                {
                    CommandCuttingFileId = item.Id
                });

                result.Created.Add(new CommandCuttingFileDto
                {
                    Id = item.Id,
                    CuttingFileId = cuttingFile.Id,
                    CuttingFileCode = cuttingFile.Code,
                    CuttingFileName = cuttingFile.Name,
                    CategoryCuttingFileId = catId,
                    CategoryCuttingFileName = category.Name,
                    ProductionCommands = productionCommands.Select(pc => new CommandProductionCommandDto
                    {
                        ProductionCommandId = pc.Id,
                        ProductionCommandCode = pc.CommandCode,
                        OrderName = pc.OrderName
                    }).ToList(),
                    Priority = (int)item.Priority,
                    PriorityName = item.Priority.ToString(),
                    MaterialCode = item.MaterialCode,
                    Length = item.Length,
                    Width = item.Width,
                    Thickness = item.Thickness,
                    Quantity = item.Quantity,
                    CuttingLength = item.CuttingLength,
                    CuttingTime = item.CuttingTime,
                    Status = (int)item.Status,
                    StatusName = item.Status.ToString(),
                    Note = item.Note,
                    Description = item.Description,
                    CreatedDate = item.CreatedDate,
                    RemainLength = item.RemainLength,
                    RemainWidth = item.RemainWidth,
                    RemainThickness = item.RemainThickness
                });
            }

            // Loại bỏ các row bị đánh lỗi trong quá trình tạo
            errorRows = rows.Where(r => !r.IsValid).ToList();
            validRows = rows.Where(r => r.IsValid).ToList();

            result.SuccessCount = validRows.Count;
            result.ErrorCount = errorRows.Count;
            result.Errors = errorRows;

            return result;
        }

        #endregion

        #region Mapping

        // [THAY ĐỔI] MapToDto: nhận ICollection<ProductionCommand> thay vì List<Guid> + Dictionary
        private CommandCuttingFileDto MapToDto(
            CommandCuttingFile main,
            CuttingFile cuttingFile,
            List<ProductionCommand> productionCommands)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var allUsers = userRepo.GetAll(x => x.IsDeleted).ToList();
            return new CommandCuttingFileDto
            {
                Id = main.Id,
                Priority = (int)main.Priority,
                PriorityName = main.Priority.ToString(),
                CuttingFileId = cuttingFile?.Id ?? main.CuttingFileId,
                CuttingFileCode = cuttingFile?.Code,
                CuttingFileName = cuttingFile?.Name,
                CategoryCuttingFileId = cuttingFile?.CategoryCuttingFileId ?? main.CategoryCuttingFileId.Value,
                CategoryCuttingFileName = cuttingFile?.CategoryCuttingFile?.Name,
                // [THAY ĐỔI] Map trực tiếp từ collection ProductionCommands
                ProductionCommands = productionCommands
                    .Select(pc => new CommandProductionCommandDto
                    {
                        ProductionCommandId = pc.Id,
                        ProductionCommandCode = pc.CommandCode,
                        OrderName = pc.OrderName
                    }).ToList(),
                MaterialCode = main.MaterialCode,
                Length = main.Length,
                Width = main.Width,
                Thickness = main.Thickness,
                Quantity = main.Quantity,
                CuttingLength = main.CuttingLength,
                CuttingTime = main.CuttingTime,
                Status = (int)main.Status,
                StatusName = main.Status.ToString(),
                PlannedQuantity = main.PlannedQuantity,
                CompletedQuantity = main.CompletedQuantity,
                Note = main.Note,
                Description = main.Description,
                CreatedDate = main.CreatedDate,
                RemainLength = main.RemainLength,
                RemainThickness = main.RemainThickness,
                RemainWidth = main.RemainWidth,
                UserName = main.CreatedBy.HasValue ? allUsers.FirstOrDefault(x => x.Id == main.CreatedBy.Value)?.UserName : null
            };
        }
        #endregion
    }
}