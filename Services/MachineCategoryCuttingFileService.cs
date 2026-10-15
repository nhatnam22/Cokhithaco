using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;

namespace CoKhiThaCo.Services
{
    public interface IMachineCategoryCuttingFileService
    {
        ICollection<MachineCategoryCuttingFileDto> GetByMachineId(Guid machineId);
        ICollection<MachineCategoryCuttingFileDto> GetByCategoryId(Guid categoryId);
        bool Assign(AssignCategoryCuttingFilesRequest request);
        bool Remove(Guid machineId, Guid categoryId);
    }

    public class MachineCategoryCuttingFileService : IMachineCategoryCuttingFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MachineCategoryCuttingFileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ICollection<MachineCategoryCuttingFileDto> GetByMachineId(Guid machineId)
        {
            var repo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();

            var machine = machineRepo.GetSingle(m => m.MachineId == machineId && !m.IsDeleted);

            return repo.GetAll(mc => mc.MachineId == machineId && !mc.IsDeleted)
                .Select(mc =>
                {
                    var cat = categoryRepo.GetSingle(c => c.Id == mc.CategoryCuttingFileId && !c.IsDeleted);
                    return new MachineCategoryCuttingFileDto
                    {
                        Id = mc.Id,
                        MachineId = mc.MachineId,
                        MachineCode = machine?.MachineCode,
                        MachineName = machine?.MachineName,
                        CategoryCuttingFileId = mc.CategoryCuttingFileId,
                        CategoryCuttingFileCode = cat?.Code,
                        CategoryCuttingFileName = cat?.Name
                    };
                }).ToList();
        }

        public ICollection<MachineCategoryCuttingFileDto> GetByCategoryId(Guid categoryId)
        {
            var repo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();

            var category = categoryRepo.GetSingle(c => c.Id == categoryId && !c.IsDeleted);

            return repo.GetAll(mc => mc.CategoryCuttingFileId == categoryId && !mc.IsDeleted)
                .Select(mc =>
                {
                    var machine = machineRepo.GetSingle(m => m.MachineId == mc.MachineId && !m.IsDeleted);
                    return new MachineCategoryCuttingFileDto
                    {
                        Id = mc.Id,
                        MachineId = mc.MachineId,
                        MachineCode = machine?.MachineCode,
                        MachineName = machine?.MachineName,
                        CategoryCuttingFileId = mc.CategoryCuttingFileId,
                        CategoryCuttingFileCode = category?.Code,
                        CategoryCuttingFileName = category?.Name
                    };
                }).ToList();
        }

        public bool Assign(AssignCategoryCuttingFilesRequest request)
        {
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            if (!machineRepo.Exists(m => m.MachineId == request.MachineId && !m.IsDeleted))
                throw new Exception("Máy không tồn tại");

            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var repo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();

            var existing = repo.GetAll(mc => mc.MachineId == request.MachineId && !mc.IsDeleted);

            foreach (var old in existing)
            {
                if (request.CategoryCuttingFileIds == null || !request.CategoryCuttingFileIds.Contains(old.CategoryCuttingFileId))
                {
                    old.IsDeleted = true;
                    old.DeletedDate = TimeHelper.Now;
                    repo.Update(old);
                }
            }

            var existingCategoryIds = existing.Select(e => e.CategoryCuttingFileId).ToHashSet();
            foreach (var categoryId in request.CategoryCuttingFileIds ?? new List<Guid>())
            {
                if (!existingCategoryIds.Contains(categoryId))
                {
                    if (!categoryRepo.Exists(c => c.Id == categoryId && !c.IsDeleted))
                        throw new Exception($"Loại file cắt {categoryId} không tồn tại");

                    repo.Add(new MachineCategoryCuttingFile
                    {
                        Id = Guid.NewGuid(),
                        MachineId = request.MachineId,
                        CategoryCuttingFileId = categoryId,
                        CreatedDate = TimeHelper.Now
                    });
                }
            }

            _unitOfWork.Complete();
            return true;
        }

        public bool Remove(Guid machineId, Guid categoryId)
        {
            var repo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var existing = repo.GetSingle(mc => mc.MachineId == machineId && mc.CategoryCuttingFileId == categoryId && !mc.IsDeleted);
            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.DeletedDate = TimeHelper.Now;
            repo.Update(existing);
            _unitOfWork.Complete();
            return true;
        }
    }
}
