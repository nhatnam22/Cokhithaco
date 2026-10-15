using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos.Position;

namespace CoKhiThaCo.Services
{
    public interface IPositionService
    {
        ICollection<PositionDto> GetAll();
        PositionDto GetById(Guid id);
        PositionDto Create(CreatePositionRequest request);
        PositionDto Update(Guid id, UpdatePositionRequest request);
        bool Delete(Guid id);
    }

    public class PositionService : IPositionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PositionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ICollection<PositionDto> GetAll()
        {
            var repo = _unitOfWork.GetRepository<Position>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();
            var permRepo = _unitOfWork.GetRepository<Permission>();

            var positions = repo.GetAll(p => !p.IsDeleted, orderBy: q => q.OrderBy(p => p.PositionName)).ToList();

            return positions.Select(p =>
            {
                var permissionIds = ppRepo.GetAll(pp => pp.PositionId == p.PositionId && !pp.IsDeleted)
                    .Select(pp => pp.PermissionId)
                    .ToHashSet();
                var codes = permRepo.GetAll(x => permissionIds.Contains(x.Id) && !x.IsDeleted)
                    .Select(x => x.Code)
                    .ToList();

                return new PositionDto
                {
                    PositionId = p.PositionId,
                    PositionName = p.PositionName,
                    Description = p.Description,
                    UserCount = userRepo.Count(u => u.PositionId == p.PositionId && !u.IsDeleted),
                    PermissionCodes = codes
                };
            }).ToList();
        }

        public PositionDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Position>();
            var position = repo.GetSingle(p => p.PositionId == id && !p.IsDeleted);
            if (position == null) return null;

            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();
            var permRepo = _unitOfWork.GetRepository<Permission>();

            var permissionIds = ppRepo.GetAll(pp => pp.PositionId == id && !pp.IsDeleted)
                .Select(pp => pp.PermissionId).ToHashSet();
            var codes = permRepo.GetAll(x => permissionIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => x.Code).ToList();

            return new PositionDto
            {
                PositionId = position.PositionId,
                PositionName = position.PositionName,
                Description = position.Description,
                UserCount = userRepo.Count(u => u.PositionId == id && !u.IsDeleted),
                PermissionCodes = codes
            };
        }

        public PositionDto Create(CreatePositionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PositionName))
                throw new Exception("Hãy nhập tên vị trí");

            var repo = _unitOfWork.GetRepository<Position>();
            if (repo.Exists(p => p.PositionName == request.PositionName && !p.IsDeleted))
                throw new Exception("Tên vị trí đã tồn tại");

            var position = new Position
            {
                PositionId = Guid.NewGuid(),
                PositionName = request.PositionName,
                Description = request.Description,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(position);
            _unitOfWork.Complete();

            return new PositionDto
            {
                PositionId = position.PositionId,
                PositionName = position.PositionName,
                Description = position.Description
            };
        }

        public PositionDto Update(Guid id, UpdatePositionRequest request)
        {
            var repo = _unitOfWork.GetRepository<Position>();
            var position = repo.GetById(id);
            if (position == null || position.IsDeleted) return null;

            if (string.IsNullOrWhiteSpace(request.PositionName))
                throw new Exception("Hãy nhập tên vị trí");

            if (repo.Exists(p => p.PositionName == request.PositionName && p.PositionId != id && !p.IsDeleted))
                throw new Exception("Tên vị trí đã tồn tại");

            position.PositionName = request.PositionName;
            position.Description = request.Description;
            position.UpdatedDate = TimeHelper.Now;

            repo.Update(position);
            _unitOfWork.Complete();

            return new PositionDto
            {
                PositionId = position.PositionId,
                PositionName = position.PositionName,
                Description = position.Description
            };
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Position>();
            var position = repo.GetById(id);
            if (position == null || position.IsDeleted) return false;

            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            if (userRepo.Exists(u => u.PositionId == id && !u.IsDeleted))
                throw new Exception("Không thể xóa vị trí đang gán cho người dùng");

            position.IsDeleted = true;
            position.DeletedDate = TimeHelper.Now;
            repo.Update(position);
            _unitOfWork.Complete();
            return true;
        }
    }
}
