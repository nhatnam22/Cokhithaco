using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CuttingFile;

namespace CoKhiThaCo.Services
{
    public interface ICuttingFileService
    {
        PagedResult<CuttingFileDto> GetPaged(CuttingFileListQuery query);
        CuttingFileDto GetById(Guid id);
        CuttingFileDto Create(CreateCuttingFileRequest request);
        CuttingFileDto Update(Guid id, UpdateCuttingFileRequest request);
        bool Delete(Guid id);
    }

    public class CuttingFileService : ICuttingFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CuttingFileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<CuttingFileDto> GetPaged(CuttingFileListQuery query)
        {
            var repo = _unitOfWork.GetRepository<CuttingFile>();
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();

            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "Code", Direction = "asc" });

            Expression<Func<CuttingFile, bool>> where = cf => !cf.IsDeleted;
            if (query.CategoryCuttingFileId.HasValue)
            {
                var catId = query.CategoryCuttingFileId.Value;
                where = cf => !cf.IsDeleted && cf.CategoryCuttingFileId == catId;
            }

            var items = repo.GetAllPaging(where, out var total, query.Page, query.PageSize, sorts, filters);

            var categories = categoryRepo.GetAll(c => !c.IsDeleted).ToDictionary(c => c.Id, c => c);

            var dtos = items.Select(cf =>
            {
                categories.TryGetValue(cf.CategoryCuttingFileId, out var cat);
                return new CuttingFileDto
                {
                    Id = cf.Id,
                    Code = cf.Code,
                    Name = cf.Name,
                    CategoryCuttingFileId = cf.CategoryCuttingFileId,
                    CategoryCuttingFileCode = cat?.Code,
                    CategoryCuttingFileName = cat?.Name,
                    CommandCount = commandFileRepo.Count(c => c.CuttingFileId == cf.Id && !c.IsDeleted)
                };
            }).ToList();

            return new PagedResult<CuttingFileDto> { Items = dtos, Total = total, Page = query.Page, PageSize = query.PageSize };
        }

        public CuttingFileDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CuttingFile>();
            var item = repo.GetSingle(cf => cf.Id == id && !cf.IsDeleted, includes: new[] { "CategoryCuttingFile" });
            if (item == null) return null;

            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();

            return new CuttingFileDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                CategoryCuttingFileId = item.CategoryCuttingFileId,
                CategoryCuttingFileCode = item.CategoryCuttingFile?.Code,
                CategoryCuttingFileName = item.CategoryCuttingFile?.Name,
                CommandCount = commandFileRepo.Count(c => c.CuttingFileId == id && !c.IsDeleted)
            };
        }

        public CuttingFileDto Create(CreateCuttingFileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                throw new Exception("Mã và tên file cắt là bắt buộc");

            var repo = _unitOfWork.GetRepository<CuttingFile>();
            if (repo.Exists(cf => cf.Code == request.Code && !cf.IsDeleted))
                throw new Exception("Mã file cắt đã tồn tại");

            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            if (!categoryRepo.Exists(c => c.Id == request.CategoryCuttingFileId && !c.IsDeleted))
                throw new Exception("Loại file cắt không tồn tại");

            var item = new CuttingFile
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                CategoryCuttingFileId = request.CategoryCuttingFileId,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(item);
            _unitOfWork.Complete();
            return GetById(item.Id);
        }

        public CuttingFileDto Update(Guid id, UpdateCuttingFileRequest request)
        {
            var repo = _unitOfWork.GetRepository<CuttingFile>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return null;

            if (repo.Exists(cf => cf.Code == request.Code && cf.Id != id && !cf.IsDeleted))
                throw new Exception("Mã file cắt đã tồn tại");

            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            if (!categoryRepo.Exists(c => c.Id == request.CategoryCuttingFileId && !c.IsDeleted))
                throw new Exception("Loại file cắt không tồn tại");

            item.Code = request.Code;
            item.Name = request.Name;
            item.CategoryCuttingFileId = request.CategoryCuttingFileId;
            item.UpdatedDate = TimeHelper.Now;

            repo.Update(item);
            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CuttingFile>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return false;

            var commandFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            if (commandFileRepo.Exists(c => c.CuttingFileId == id && !c.IsDeleted))
                throw new Exception("Không thể xóa file cắt có chứa các lệnh");

            item.IsDeleted = true;
            item.DeletedDate = TimeHelper.Now;
            repo.Update(item);
            _unitOfWork.Complete();
            return true;
        }
    }
}
