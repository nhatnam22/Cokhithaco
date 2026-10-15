using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CategoryCuttingFile;

namespace CoKhiThaCo.Services
{
    public interface ICategoryCuttingFileService
    {
        PagedResult<CategoryCuttingFileDto> GetPaged(CategoryCuttingFileListQuery query, bool? IsMaterial);
        CategoryCuttingFileDto GetById(Guid id);
        CategoryCuttingFileDto Create(CreateCategoryCuttingFileRequest request);
        CategoryCuttingFileDto Update(Guid id, UpdateCategoryCuttingFileRequest request);
        bool Delete(Guid id);
    }

    public class CategoryCuttingFileService : ICategoryCuttingFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryCuttingFileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<CategoryCuttingFileDto> GetPaged(CategoryCuttingFileListQuery query, bool? IsMaterial)
        {
            var repo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var cuttingFileRepo = _unitOfWork.GetRepository<CuttingFile>();
            var machineCategoryRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var IsHasKeyword = string.IsNullOrEmpty(query.Keyword);

            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "Name", Direction = "asc" });

            var items = repo.GetAllPaging(c => !c.IsDeleted && (!IsMaterial.HasValue || c.IsMaterial == IsMaterial) && (IsHasKeyword || c.Code == query.Keyword || c.Name == query.Keyword), out var total, query.Page, query.PageSize, sorts, filters);

            var dtos = items.Select(c => new CategoryCuttingFileDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                CuttingFileCount = cuttingFileRepo.Count(cf => cf.CategoryCuttingFileId == c.Id && !cf.IsDeleted),
                MachineCount = machineCategoryRepo.Count(mc => mc.CategoryCuttingFileId == c.Id && !mc.IsDeleted),
                IsMaterial = c.IsMaterial,
            }).ToList();

            return new PagedResult<CategoryCuttingFileDto> { Items = dtos, Total = total, Page = query.Page, PageSize = query.PageSize };
        }

        public CategoryCuttingFileDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var item = repo.GetSingle(c => c.Id == id && !c.IsDeleted);
            if (item == null) return null;

            var cuttingFileRepo = _unitOfWork.GetRepository<CuttingFile>();
            var machineCategoryRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();

            return new CategoryCuttingFileDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsMaterial = item.IsMaterial,
                CuttingFileCount = cuttingFileRepo.Count(cf => cf.CategoryCuttingFileId == id && !cf.IsDeleted),
                MachineCount = machineCategoryRepo.Count(mc => mc.CategoryCuttingFileId == id && !mc.IsDeleted)
            };
        }

        public CategoryCuttingFileDto Create(CreateCategoryCuttingFileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new Exception("Hãy nhập mã danh mục");

            var repo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            if (repo.Exists(c => c.Code == request.Code && !c.IsDeleted))
                throw new Exception("Mã danh mục đã tồn tại");

            var item = new CategoryCuttingFile
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                CreatedDate = TimeHelper.Now,
                IsMaterial = request.IsMaterial
            };

            repo.Add(item);
            _unitOfWork.Complete();
            return GetById(item.Id);
        }

        public CategoryCuttingFileDto Update(Guid id, UpdateCategoryCuttingFileRequest request)
        {
            var repo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return null;

            if (repo.Exists(c => c.Code == request.Code && c.Id != id && !c.IsDeleted))
                throw new Exception("Danh mục với mã này đã tồn tại");

            item.Code = request.Code;
            item.Name = request.Name;
            item.Description = request.Description;
            item.UpdatedDate = TimeHelper.Now;
            item.IsMaterial = request.IsMaterial;

            repo.Update(item);
            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var item = repo.GetById(id);
            if (item == null || item.IsDeleted) return false;

            var cuttingFileRepo = _unitOfWork.GetRepository<CuttingFile>();
            if (cuttingFileRepo.Exists(cf => cf.CategoryCuttingFileId == id && !cf.IsDeleted))
                throw new Exception("Không thể xóa danh mục có chứa tệp cắt");

            item.IsDeleted = true;
            item.DeletedDate = TimeHelper.Now;
            repo.Update(item);
            _unitOfWork.Complete();
            return true;
        }
    }
}
