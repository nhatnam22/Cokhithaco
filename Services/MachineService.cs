using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;
using CoKhiThaCo.Models.Dtos.WorkShift;
using CoKhiThaCo.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoKhiThaCo.Services
{
    public interface IMachineService
    {
        PagedResult<MachineDto> GetPaged(MachineListQuery query);
        MachineDto GetById(Guid id);
        MachineDto Create(CreateMachineRequest request);
        MachineDto Update(Guid id, UpdateMachineRequest request);
        bool Delete(Guid id);
        bool IsOperatorOfMachine(Guid userId, Guid machineId);
        bool CanHandleCategory(Guid machineId, Guid categoryCuttingFileId);
        bool IsMachineAvailable(Guid machineId, DateTime startTime, DateTime endTime, Guid? excludePlanningId = null);
        void AssignWorkShifts(AssignWorkShiftsToMachineRequest request);
        void RemoveWorkShift(Guid machineId, Guid workShiftId);
        void AssignCategoryCuttingFiles(AssignCategoriesToMachineRequest request);
        void RemoveCategoryCuttingFile(Guid machineId, Guid categoryCuttingFileId);
    }

    public class MachineService : IMachineService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MachineService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<MachineDto> GetPaged(MachineListQuery query)
        {
            var repo = _unitOfWork.GetRepository<Machine>();
            var operatorRepo = _unitOfWork.GetRepository<MachineOperator>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var machineCategoryRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var workShiftMachineRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
            var workShiftRepo = _unitOfWork.GetRepository<WorkShift>();
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var commandCuttingFileRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var machineCategoryCuttingFileRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();

            var isHasSearch = string.IsNullOrEmpty(query.Keyword);
            var activeMachines = new List<Guid>();
            if (query.IsChooseMachinePlan && !string.IsNullOrWhiteSpace(query.CommandCuttingFileSearch))
            {
                var commandCuttingFileId = Guid.Parse(query.CommandCuttingFileSearch);
                //var plans = planningRepo.GetAll(whereCondition: p => !p.IsDeleted 
                //    && p.Status != PlanningStatus.Cancelled && p.Status != PlanningStatus.Completed);
                var currentCommandCuttingFile = commandCuttingFileRepo.GetSingle(c => c.Id == commandCuttingFileId);
                if (currentCommandCuttingFile == null) throw new Exception("Không tìm thấy lệnh file cắt");
                //activeMachines = plans.Where(p => p.MachineId.HasValue).Select(p => p.MachineId.Value).ToList();
                var machineBelongtoCategoryFiles = machineCategoryCuttingFileRepo.GetAll(x => x.CategoryCuttingFileId == currentCommandCuttingFile.CategoryCuttingFileId)
                    .Select(m => m.MachineId).ToList();
                activeMachines = machineBelongtoCategoryFiles.Except(activeMachines).ToList();
            }
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "MachineCode", Direction = "asc" });

            var items = repo.GetAllPaging(m => !m.IsDeleted && (activeMachines == null || !activeMachines.Any() || activeMachines.Contains(m.MachineId)) 
            && (isHasSearch || m.MachineCode.Contains(query.Keyword) || m.MachineName.Contains(query.Keyword)), out var total, query.Page, query.PageSize, sorts, filters);

            var workShiftMachines = workShiftMachineRepo.GetAll(
                whereCondition: wsm => !wsm.IsDeleted && items.Select(i => i.MachineId).Contains(wsm.MachineId),
                includes: new[] { "WorkShift" }
                ).GroupBy(wsm => wsm.MachineId).ToDictionary(g => g.Key, g => g.ToList());

            var dtos = new List<MachineDto>();
            foreach (var m in items)
            {
                var operators = operatorRepo.GetAll(o => o.MachineId == m.MachineId && !o.IsDeleted)
                    .Select(o =>
                    {
                        var u = userRepo.GetSingle(x => x.Id == o.UserId && !x.IsDeleted);
                        return new MachineOperatorDto
                        {
                            Id = o.Id,
                            UserId = o.UserId,
                            UserName = u?.UserName,
                            FullName = u?.FullName,
                            MachineId = o.MachineId
                        };
                    }).ToList();

                var categories = machineCategoryRepo.GetAll(mc => mc.MachineId == m.MachineId && !mc.IsDeleted)
                    .Select(mc =>
                    {
                        var c = categoryRepo.GetSingle(x => x.Id == mc.CategoryCuttingFileId && !x.IsDeleted);
                        return new CategoryCuttingFileDto
                        {
                            Id = mc.CategoryCuttingFileId,
                            Code = c?.Code,
                            Name = c?.Name
                        };
                    }).ToList();

                var workShifts = workShiftMachines.ContainsKey(m.MachineId)
                    ? workShiftMachines[m.MachineId].Select(wsm => new WorkShiftDto
                    {
                        Id = wsm.WorkShiftId,
                        ShiftCode = wsm.WorkShift?.ShiftCode,
                        ShiftName = wsm.WorkShift?.ShiftName,
                        StartTime = wsm.WorkShift?.StartTime ?? TimeSpan.Zero,
                        EndTime = wsm.WorkShift?.EndTime ?? TimeSpan.Zero,
                        StandardTimeMinutes = wsm.WorkShift?.StandardTimeMinutes ?? 0
                    }).ToList()
                    : new List<WorkShiftDto>();

                dtos.Add(new MachineDto
                {
                    MachineId = m.MachineId,
                    MachineCode = m.MachineCode,
                    MachineName = m.MachineName,
                    Status = (int)m.Status,
                    StatusName = m.Status.ToString(),
                    PowerKw = m.PowerKw,
                    IsReserveMachine = m.IsReserveMachine,
                    MinCutLengthMm = m.MinCutLengthMm,
                    MaxCutLengthMm = m.MaxCutLengthMm,
                    MinCutThicknessMm = m.MinCutThicknessMm,
                    MaxCutThicknessMm = m.MaxCutThicknessMm,
                    PerformancePercent = m.PerformancePercent,
                    Operators = operators,
                    CategoryCuttingFiles = categories,
                    WorkShifts = workShifts
                });
            }

            return new PagedResult<MachineDto> { Items = dtos, Total = total, Page = query.Page, PageSize = query.PageSize };
        }

        public MachineDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Machine>();
            var machine = repo.GetSingle(m => m.MachineId == id && !m.IsDeleted);
            if (machine == null) return null;

            var operatorRepo = _unitOfWork.GetRepository<MachineOperator>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var machineCategoryRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var categoryRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var workShiftMachineRepo = _unitOfWork.GetRepository<WorkShiftMachine>();

            var operators = operatorRepo.GetAll(o => o.MachineId == id && !o.IsDeleted)
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
                        MachineCode = machine.MachineCode
                    };
                }).ToList();

            var categories = machineCategoryRepo.GetAll(mc => mc.MachineId == id && !mc.IsDeleted)
                .Select(mc =>
                {
                    var c = categoryRepo.GetSingle(x => x.Id == mc.CategoryCuttingFileId && !x.IsDeleted);
                    return new CategoryCuttingFileDto
                    {
                        Id = mc.CategoryCuttingFileId,
                        Code = c?.Code,
                        Name = c?.Name
                    };
                }).ToList();

            var workShifts = workShiftMachineRepo.GetAll(
                whereCondition: wsm => wsm.MachineId == id && !wsm.IsDeleted,
                includes: new[] { "WorkShift" }
                ).Select(wsm => new WorkShiftDto
                {
                    Id = wsm.WorkShiftId,
                    ShiftCode = wsm.WorkShift?.ShiftCode,
                    ShiftName = wsm.WorkShift?.ShiftName,
                    StartTime = wsm.WorkShift?.StartTime ?? TimeSpan.Zero,
                    EndTime = wsm.WorkShift?.EndTime ?? TimeSpan.Zero,
                    StandardTimeMinutes = wsm.WorkShift?.StandardTimeMinutes ?? 0
                }).ToList();

            return new MachineDto
            {
                MachineId = machine.MachineId,
                MachineCode = machine.MachineCode,
                MachineName = machine.MachineName,
                Status = (int)machine.Status,
                StatusName = machine.Status.ToString(),
                PowerKw = machine.PowerKw,
                IsReserveMachine = machine.IsReserveMachine,
                MinCutLengthMm = machine.MinCutLengthMm,
                MaxCutLengthMm = machine.MaxCutLengthMm,
                MinCutThicknessMm = machine.MinCutThicknessMm,
                MaxCutThicknessMm = machine.MaxCutThicknessMm,
                PerformancePercent = machine.PerformancePercent,
                Operators = operators,
                CategoryCuttingFiles = categories,
                WorkShifts = workShifts
            };
        }

        public MachineDto Create(CreateMachineRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MachineCode) || string.IsNullOrWhiteSpace(request.MachineName))
                throw new Exception("Mã máy và tên máy là bắt buộc");

            var repo = _unitOfWork.GetRepository<Machine>();
            if (repo.Exists(m => m.MachineCode == request.MachineCode && !m.IsDeleted))
                throw new Exception("Mã máy đã tồn tại");

            var machine = new Machine
            {
                MachineId = Guid.NewGuid(),
                MachineCode = request.MachineCode,
                MachineName = request.MachineName,
                PowerKw = request.PowerKw,
                IsReserveMachine = request.IsReserveMachine,
                MinCutLengthMm = request.MinCutLengthMm,
                MaxCutLengthMm = request.MaxCutLengthMm,
                MinCutThicknessMm = request.MinCutThicknessMm,
                MaxCutThicknessMm = request.MaxCutThicknessMm,
                PerformancePercent = request.PerformancePercent,
                Status = MachineStatus.Active,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(machine);

            // Gán ca làm việc
            if (request.WorkShiftIds != null && request.WorkShiftIds.Any())
            {
                var wsmRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
                var wsRepo = _unitOfWork.GetRepository<WorkShift>();
                foreach (var wsId in request.WorkShiftIds)
                {
                    if (wsRepo.Exists(w => w.Id == wsId && !w.IsDeleted))
                    {
                        wsmRepo.Add(new WorkShiftMachine
                        {
                            Id = Guid.NewGuid(),
                            MachineId = machine.MachineId,
                            WorkShiftId = wsId,
                            CreatedDate = TimeHelper.Now
                        });
                    }
                }
            }

            // Gán loại file cắt
            if (request.CategoryCuttingFileIds != null && request.CategoryCuttingFileIds.Any())
            {
                var mccRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
                var catRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
                foreach (var catId in request.CategoryCuttingFileIds)
                {
                    if (catRepo.Exists(c => c.Id == catId && !c.IsDeleted))
                    {
                        mccRepo.Add(new MachineCategoryCuttingFile
                        {
                            Id = Guid.NewGuid(),
                            MachineId = machine.MachineId,
                            CategoryCuttingFileId = catId,
                            CreatedDate = TimeHelper.Now
                        });
                    }
                }
            }

            _unitOfWork.Complete();
            return GetById(machine.MachineId);
        }

        public MachineDto Update(Guid id, UpdateMachineRequest request)
        {
            var repo = _unitOfWork.GetRepository<Machine>();
            var machine = repo.GetById(id);
            if (machine == null || machine.IsDeleted) return null;

            if (repo.Exists(m => m.MachineCode == request.MachineCode && m.MachineId != id && !m.IsDeleted))
                throw new Exception("Mã máy đã tồn tại");

            machine.MachineCode = request.MachineCode;
            machine.MachineName = request.MachineName;
            machine.Status = (MachineStatus)request.Status;
            machine.PowerKw = request.PowerKw;
            machine.IsReserveMachine = request.IsReserveMachine;
            machine.MinCutLengthMm = request.MinCutLengthMm;
            machine.MaxCutLengthMm = request.MaxCutLengthMm;
            machine.MinCutThicknessMm = request.MinCutThicknessMm;
            machine.MaxCutThicknessMm = request.MaxCutThicknessMm;
            machine.PerformancePercent = request.PerformancePercent;
            machine.UpdatedDate = TimeHelper.Now;

            repo.Update(machine);

            // Cập nhật ca làm việc
            if (request.WorkShiftIds != null)
            {
                var wsmRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
                var wsRepo = _unitOfWork.GetRepository<WorkShift>();
                var existing = wsmRepo.GetAll(wsm => wsm.MachineId == id && !wsm.IsDeleted).ToList();

                // Xóa các ca không còn trong danh sách
                foreach (var item in existing)
                {
                    if (!request.WorkShiftIds.Contains(item.WorkShiftId))
                    {
                        item.IsDeleted = true;
                        item.DeletedDate = TimeHelper.Now;
                        wsmRepo.Update(item);
                    }
                }

                // Thêm các ca mới
                var existingIds = existing.Select(e => e.WorkShiftId).ToList();
                foreach (var wsId in request.WorkShiftIds)
                {
                    if (!existingIds.Contains(wsId) && wsRepo.Exists(w => w.Id == wsId && !w.IsDeleted))
                    {
                        wsmRepo.Add(new WorkShiftMachine
                        {
                            Id = Guid.NewGuid(),
                            MachineId = id,
                            WorkShiftId = wsId,
                            CreatedDate = TimeHelper.Now
                        });
                    }
                }
            }

            // Cập nhật loại file cắt
            if (request.CategoryCuttingFileIds != null)
            {
                var mccRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
                var catRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
                var existing = mccRepo.GetAll(mc => mc.MachineId == id && !mc.IsDeleted).ToList();

                // Xóa các loại không còn trong danh sách
                foreach (var item in existing)
                {
                    if (!request.CategoryCuttingFileIds.Contains(item.CategoryCuttingFileId))
                    {
                        item.IsDeleted = true;
                        item.DeletedDate = TimeHelper.Now;
                        mccRepo.Update(item);
                    }
                }

                // Thêm các loại mới
                var existingIds = existing.Select(e => e.CategoryCuttingFileId).ToList();
                foreach (var catId in request.CategoryCuttingFileIds)
                {
                    if (!existingIds.Contains(catId) && catRepo.Exists(c => c.Id == catId && !c.IsDeleted))
                    {
                        mccRepo.Add(new MachineCategoryCuttingFile
                        {
                            Id = Guid.NewGuid(),
                            MachineId = id,
                            CategoryCuttingFileId = catId,
                            CreatedDate = TimeHelper.Now
                        });
                    }
                }
            }

            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Machine>();
            var machine = repo.GetById(id);
            if (machine == null || machine.IsDeleted) return false;

            var planningRepo = _unitOfWork.GetRepository<Planning>();
            if (planningRepo.Exists(p => p.MachineId == id && !p.IsDeleted && p.Status != PlanningStatus.Completed && p.Status != PlanningStatus.Cancelled))
                throw new Exception("Không thể xóa máy có kế hoạch đang hoạt động");

            machine.IsDeleted = true;
            machine.DeletedDate = TimeHelper.Now;
            repo.Update(machine);
            _unitOfWork.Complete();
            return true;
        }

        public bool IsOperatorOfMachine(Guid userId, Guid machineId)
        {
            var repo = _unitOfWork.GetRepository<MachineOperator>();
            return repo.Exists(mo => mo.UserId == userId && mo.MachineId == machineId && !mo.IsDeleted);
        }

        public bool CanHandleCategory(Guid machineId, Guid categoryCuttingFileId)
        {
            var repo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            return repo.Exists(mc => mc.MachineId == machineId && mc.CategoryCuttingFileId == categoryCuttingFileId && !mc.IsDeleted);
        }

        public bool IsMachineAvailable(Guid machineId, DateTime startTime, DateTime endTime, Guid? excludePlanningId = null)
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var overlapExists = planningRepo.Exists(p =>
                p.MachineId == machineId &&
                !p.IsDeleted &&
                p.Status != PlanningStatus.Cancelled &&
                p.Id != excludePlanningId &&
                p.StartTime < endTime &&
                p.EndTime > startTime);
            return !overlapExists;
        }

        public void AssignWorkShifts(AssignWorkShiftsToMachineRequest request)
        {
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            if (!machineRepo.Exists(m => m.MachineId == request.MachineId && !m.IsDeleted))
                throw new Exception("Máy không tồn tại");

            var wsRepo = _unitOfWork.GetRepository<WorkShift>();
            var wsmRepo = _unitOfWork.GetRepository<WorkShiftMachine>();

            foreach (var wsId in request.WorkShiftIds)
            {
                if (!wsRepo.Exists(w => w.Id == wsId && !w.IsDeleted))
                    throw new Exception($"Ca làm việc {wsId} không tồn tại");
            }

            var existing = wsmRepo.GetAll(wsm => wsm.MachineId == request.MachineId && !wsm.IsDeleted).ToList();

            // Xóa các ca không còn trong danh sách
            foreach (var item in existing)
            {
                if (!request.WorkShiftIds.Contains(item.WorkShiftId))
                {
                    item.IsDeleted = true;
                    item.DeletedDate = TimeHelper.Now;
                    wsmRepo.Update(item);
                }
            }

            // Thêm các ca mới
            var existingIds = existing.Select(e => e.WorkShiftId).ToList();
            foreach (var wsId in request.WorkShiftIds)
            {
                if (!existingIds.Contains(wsId))
                {
                    wsmRepo.Add(new WorkShiftMachine
                    {
                        Id = Guid.NewGuid(),
                        MachineId = request.MachineId,
                        WorkShiftId = wsId,
                        CreatedDate = TimeHelper.Now
                    });
                }
            }

            _unitOfWork.Complete();
        }

        public void RemoveWorkShift(Guid machineId, Guid workShiftId)
        {
            var wsmRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
            var wsm = wsmRepo.GetSingle(w => w.MachineId == machineId && w.WorkShiftId == workShiftId && !w.IsDeleted);
            if (wsm == null)
                throw new Exception("Không tìm thấy bản ghi phân công ca làm việc cho máy");

            wsm.IsDeleted = true;
            wsm.DeletedDate = TimeHelper.Now;
            wsmRepo.Update(wsm);
            _unitOfWork.Complete();
        }

        public void AssignCategoryCuttingFiles(AssignCategoriesToMachineRequest request)
        {
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            if (!machineRepo.Exists(m => m.MachineId == request.MachineId && !m.IsDeleted))
                throw new Exception("Máy không tồn tại");

            var catRepo = _unitOfWork.GetRepository<CategoryCuttingFile>();
            var mccRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();

            foreach (var catId in request.CategoryCuttingFileIds)
            {
                if (!catRepo.Exists(c => c.Id == catId && !c.IsDeleted))
                    throw new Exception($"Loại file cắt {catId} không tồn tại");
            }

            var existing = mccRepo.GetAll(mc => mc.MachineId == request.MachineId && !mc.IsDeleted).ToList();

            // Xóa các loại không còn trong danh sách
            foreach (var item in existing)
            {
                if (!request.CategoryCuttingFileIds.Contains(item.CategoryCuttingFileId))
                {
                    item.IsDeleted = true;
                    item.DeletedDate = TimeHelper.Now;
                    mccRepo.Update(item);
                }
            }

            // Thêm các loại mới
            var existingIds = existing.Select(e => e.CategoryCuttingFileId).ToList();
            foreach (var catId in request.CategoryCuttingFileIds)
            {
                if (!existingIds.Contains(catId))
                {
                    mccRepo.Add(new MachineCategoryCuttingFile
                    {
                        Id = Guid.NewGuid(),
                        MachineId = request.MachineId,
                        CategoryCuttingFileId = catId,
                        CreatedDate = TimeHelper.Now
                    });
                }
            }

            _unitOfWork.Complete();
        }

        public void RemoveCategoryCuttingFile(Guid machineId, Guid categoryCuttingFileId)
        {
            var mccRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var mcc = mccRepo.GetSingle(mc => mc.MachineId == machineId && mc.CategoryCuttingFileId == categoryCuttingFileId && !mc.IsDeleted);
            if (mcc == null)
                throw new Exception("Không tìm thấy bản ghi phân công loại file cắt cho máy");

            mcc.IsDeleted = true;
            mcc.DeletedDate = TimeHelper.Now;
            mccRepo.Update(mcc);
            _unitOfWork.Complete();
        }
    }
}