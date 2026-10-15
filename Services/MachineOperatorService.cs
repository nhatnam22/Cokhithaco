using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;

namespace CoKhiThaCo.Services
{
    public interface IMachineOperatorService
    {
        ICollection<MachineOperatorDto> GetMachinesByUserId(Guid userId);
        ICollection<MachineOperatorDto> GetUsersByMachineId(Guid machineId);
        bool AssignMachinesToUser(AssignMachinesToUserRequest request);
        bool Remove(Guid machineId, Guid userId);
    }

    public class MachineOperatorService : IMachineOperatorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MachineOperatorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ICollection<MachineOperatorDto> GetMachinesByUserId(Guid userId)
        {
            var repo = _unitOfWork.GetRepository<MachineOperator>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var machineRepo = _unitOfWork.GetRepository<Machine>();

            var user = userRepo.GetSingle(u => u.Id == userId && !u.IsDeleted);
            return repo.GetAll(o => o.UserId == userId && !o.IsDeleted)
                .Select(o =>
                {
                    var m = machineRepo.GetSingle(x => x.MachineId == o.MachineId && !x.IsDeleted);
                    return new MachineOperatorDto
                    {
                        Id = o.Id,
                        UserId = o.UserId,
                        UserName = user?.UserName,
                        FullName = user?.FullName,
                        MachineId = o.MachineId,
                        MachineCode = m?.MachineCode
                    };
                }).ToList();
        }

        public ICollection<MachineOperatorDto> GetUsersByMachineId(Guid machineId)
        {
            var repo = _unitOfWork.GetRepository<MachineOperator>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var machineRepo = _unitOfWork.GetRepository<Machine>();

            var machine = machineRepo.GetSingle(m => m.MachineId == machineId && !m.IsDeleted);
            return repo.GetAll(o => o.MachineId == machineId && !o.IsDeleted)
                .Select(o =>
                {
                    var u = userRepo.GetSingle(x => x.Id == o.UserId && !x.IsDeleted);
                    return new MachineOperatorDto
                    {
                        Id = o.Id,
                        UserId = o.UserId,
                        UserName = u?.UserName,
                        FullName = u?.FullName,
                        MachineId = o.MachineId,
                        MachineCode = machine?.MachineCode
                    };
                }).ToList();
        }

        public bool AssignMachinesToUser(AssignMachinesToUserRequest request)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            if (!userRepo.Exists(u => u.Id == request.UserId && !u.IsDeleted))
                throw new Exception("Người dùng không tồn tại");

            var machineRepo = _unitOfWork.GetRepository<Machine>();
            var repo = _unitOfWork.GetRepository<MachineOperator>();

            var existing = repo.GetAll(o => o.UserId == request.UserId && !o.IsDeleted);

            // Xóa các máy không còn trong danh sách
            foreach (var old in existing)
            {
                if (request.MachineIds == null || !request.MachineIds.Contains(old.MachineId))
                {
                    old.IsDeleted = true;
                    old.DeletedDate = TimeHelper.Now;
                    repo.Update(old);
                }
            }

            // Thêm các máy mới
            var existingMachineIds = existing.Select(e => e.MachineId).ToHashSet();
            foreach (var machineId in request.MachineIds ?? new List<Guid>())
            {
                if (!existingMachineIds.Contains(machineId))
                {
                    var machine = machineRepo.GetSingle(m => m.MachineId == machineId && !m.IsDeleted);
                    if (machine == null)
                        throw new Exception($"Máy {machineId} không tồn tại");

                    repo.Add(new MachineOperator
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        MachineId = machineId,
                        CreatedDate = TimeHelper.Now
                    });
                }
            }

            _unitOfWork.Complete();
            return true;
        }

        public bool Remove(Guid machineId, Guid userId)
        {
            var repo = _unitOfWork.GetRepository<MachineOperator>();
            var existing = repo.GetSingle(o => o.MachineId == machineId && o.UserId == userId && !o.IsDeleted);
            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.DeletedDate = TimeHelper.Now;
            repo.Update(existing);
            _unitOfWork.Complete();
            return true;
        }
    }
}