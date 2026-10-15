using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.WorkShift;

namespace CoKhiThaCo.Services
{
    public interface IWorkShiftService
    {
        PagedResult<WorkShiftDto> GetPaged(WorkShiftListQuery query);
        WorkShiftDto GetById(Guid id);
        WorkShiftDto Create(CreateWorkShiftRequest request);
        WorkShiftDto Update(Guid id, UpdateWorkShiftRequest request);
        bool Delete(Guid id);
        ShiftBreakDto CreateShiftBreak(CreateShiftBreakRequest request);
        ShiftBreakDto UpdateShiftBreak(Guid id, UpdateShiftBreakRequest request);
        bool DeleteShiftBreak(Guid id);
        List<ShiftBreakDto> GetShiftBreaks(Guid workShiftId);
    }

    public class WorkShiftService : IWorkShiftService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkShiftService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<WorkShiftDto> GetPaged(WorkShiftListQuery query)
        {
            var repo = _unitOfWork.GetRepository<WorkShift>();
            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();

            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "ShiftCode", Direction = "asc" });

            var items = repo.GetAllPaging(w => !w.IsDeleted, out var total, query.Page, query.PageSize, sorts, filters);

            var dtos = new List<WorkShiftDto>();
            foreach (var ws in items)
            {
                var breaks = breakRepo.GetAll(b => b.WorkShiftId == ws.Id && !b.IsDeleted)
                    .Select(b => new ShiftBreakDto
                    {
                        Id = b.Id,
                        WorkShiftId = b.WorkShiftId,
                        BreakName = b.BreakName,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        DurationMinutes = b.DurationMinutes
                    }).ToList();

                dtos.Add(new WorkShiftDto
                {
                    Id = ws.Id,
                    ShiftCode = ws.ShiftCode,
                    ShiftName = ws.ShiftName,
                    StartTime = ws.StartTime,
                    EndTime = ws.EndTime,
                    StandardTimeMinutes = ws.StandardTimeMinutes,
                    Breaks = breaks
                });
            }

            return new PagedResult<WorkShiftDto> { Items = dtos, Total = total, Page = query.Page, PageSize = query.PageSize };
        }

        public WorkShiftDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<WorkShift>();
            var ws = repo.GetSingle(w => w.Id == id && !w.IsDeleted);
            if (ws == null) return null;

            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();

            var breaks = breakRepo.GetAll(b => b.WorkShiftId == id && !b.IsDeleted)
                .Select(b => new ShiftBreakDto
                {
                    Id = b.Id,
                    WorkShiftId = b.WorkShiftId,
                    BreakName = b.BreakName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    DurationMinutes = b.DurationMinutes
                }).ToList();

            return new WorkShiftDto
            {
                Id = ws.Id,
                ShiftCode = ws.ShiftCode,
                ShiftName = ws.ShiftName,
                StartTime = ws.StartTime,
                EndTime = ws.EndTime,
                StandardTimeMinutes = ws.StandardTimeMinutes,
                Breaks = breaks
            };
        }

        public WorkShiftDto Create(CreateWorkShiftRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ShiftCode) || string.IsNullOrWhiteSpace(request.ShiftName))
                throw new ArgumentException("Mã ca làm việc và tên ca làm việc là bắt buộc");

            var repo = _unitOfWork.GetRepository<WorkShift>();
            if (repo.Exists(w => w.ShiftCode == request.ShiftCode && !w.IsDeleted))
                throw new ArgumentException("Mã ca làm việc đã tồn tại");
            var workShift = new WorkShift
            {
                Id = Guid.NewGuid(),
                ShiftCode = request.ShiftCode,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CreatedDate = TimeHelper.Now
            };
            var totalShiftMinutes = (decimal)request.EndTime.Subtract(request.StartTime).TotalMinutes;
            if (totalShiftMinutes < 0)
            {
                totalShiftMinutes += 24 * 60;
            }

            decimal totalBreakDuration = 0;
            if (request.ShiftBreaks != null && request.ShiftBreaks.Any())
            {
                var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();
                foreach (var breakRequest in request.ShiftBreaks)
                {
                    if(breakRequest.StartTime >= breakRequest.EndTime || request.StartTime == TimeSpan.Zero || request.EndTime == TimeSpan.Zero)
                        throw new ArgumentException("Thời gian bắt đầu và kết thúc của khoảng nghỉ là bắt buộc");
                    var breakMinutes = (decimal)breakRequest.EndTime.Subtract(breakRequest.StartTime).TotalMinutes;
                    if (breakMinutes < 0)
                    {
                        breakMinutes += 24 * 60;
                    }
                    totalBreakDuration += breakMinutes;

                    var shiftBreak = new ShiftBreak
                    {
                        Id = Guid.NewGuid(),
                        WorkShiftId = workShift.Id,
                        BreakName = breakRequest.BreakName,
                        StartTime = breakRequest.StartTime,
                        EndTime = breakRequest.EndTime,
                        DurationMinutes = breakMinutes,
                        CreatedDate = TimeHelper.Now
                    };

                    breakRepo.Add(shiftBreak);
                }
            }
            workShift.StandardTimeMinutes = totalShiftMinutes - totalBreakDuration;
            repo.Add(workShift);
            _unitOfWork.Complete();
            return GetById(workShift.Id);
        }

        public WorkShiftDto Update(Guid id, UpdateWorkShiftRequest request)
        {
            var repo = _unitOfWork.GetRepository<WorkShift>();
            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();

            var ws = repo.GetById(id);
            if (ws == null || ws.IsDeleted) return null;

            if (repo.Exists(w => w.ShiftCode == request.ShiftCode && w.Id != id && !w.IsDeleted))
                throw new ArgumentException("Mã ca làm việc đã tồn tại");

            ws.ShiftCode = request.ShiftCode;
            ws.ShiftName = request.ShiftName;
            ws.StartTime = request.StartTime;
            ws.EndTime = request.EndTime;
            ws.UpdatedDate = TimeHelper.Now;

            var totalShiftMinutes = (decimal)request.EndTime.Subtract(request.StartTime).TotalMinutes;
            if (totalShiftMinutes < 0) totalShiftMinutes += 24 * 60;

            decimal totalBreakDuration = 0;

            var existingBreaks = breakRepo.GetAll(b => b.WorkShiftId == id && !b.IsDeleted).ToList();

            if (request.ShiftBreaks != null && request.ShiftBreaks.Any())
            {
                var requestBreakIds = request.ShiftBreaks
                    .Where(b => b.Id.HasValue && b.Id.Value != Guid.Empty)
                    .Select(b => b.Id.Value)
                    .ToList();
                var breaksToRemove = existingBreaks.Where(b => !requestBreakIds.Contains(b.Id)).ToList();
                foreach (var breakToRemove in breaksToRemove)
                {
                    breakToRemove.IsDeleted = true;
                    breakToRemove.UpdatedDate = TimeHelper.Now;
                    breakRepo.Update(breakToRemove);
                }

                foreach (var breakReq in request.ShiftBreaks)
                {
                    // Tính thời gian của khoản nghỉ này
                    var breakMinutes = (decimal)breakReq.EndTime.Subtract(breakReq.StartTime).TotalMinutes;
                    if (breakMinutes < 0) breakMinutes += 24 * 60;

                    totalBreakDuration += breakMinutes;

                    if (breakReq.Id.HasValue && breakReq.Id.Value != Guid.Empty)
                    {
                        // Cập nhật khoảng nghỉ hiện tại
                        var existingBreak = existingBreaks.FirstOrDefault(b => b.Id == breakReq.Id.Value);
                        if (existingBreak != null)
                        {
                            existingBreak.BreakName = breakReq.BreakName;
                            existingBreak.StartTime = breakReq.StartTime;
                            existingBreak.EndTime = breakReq.EndTime;
                            existingBreak.DurationMinutes = breakMinutes;
                            existingBreak.UpdatedDate = TimeHelper.Now;

                            breakRepo.Update(existingBreak);
                        }
                    }
                    else
                    {
                        var newBreak = new ShiftBreak
                        {
                            Id = Guid.NewGuid(),
                            WorkShiftId = ws.Id,
                            BreakName = breakReq.BreakName,
                            StartTime = breakReq.StartTime,
                            EndTime = breakReq.EndTime,
                            DurationMinutes = breakMinutes,
                            CreatedDate = TimeHelper.Now
                        };

                        breakRepo.Add(newBreak);
                    }
                }
            }
            else
            {
                foreach (var breakItem in existingBreaks)
                {
                    breakItem.IsDeleted = true;
                    breakItem.UpdatedDate = TimeHelper.Now;
                    breakRepo.Update(breakItem);
                }
            }

            ws.StandardTimeMinutes = totalShiftMinutes - totalBreakDuration;
            if (ws.StandardTimeMinutes < 0) ws.StandardTimeMinutes = 0;

            repo.Update(ws);
            _unitOfWork.Complete();

            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<WorkShift>();
            var ws = repo.GetById(id);
            if (ws == null || ws.IsDeleted) return false;

            // Ca làm việc chỉ còn liên kết gián tiếp qua WorkShiftMachine, không còn liên kết trực tiếp trên Planning
            var wsmRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
            if (wsmRepo.Exists(wsm => wsm.WorkShiftId == id && !wsm.IsDeleted))
                throw new Exception("Không thể xóa ca làm việc đang được phân công cho máy");

            ws.IsDeleted = true;
            ws.DeletedDate = TimeHelper.Now;
            repo.Update(ws);
            _unitOfWork.Complete();
            return true;
        }

        public ShiftBreakDto CreateShiftBreak(CreateShiftBreakRequest request)
        {
            var duration = (decimal)(request.EndTime - request.StartTime).TotalMinutes;
            if (duration <= 0)
                throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu");

            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();
            var shiftBreak = new ShiftBreak
            {
                Id = Guid.NewGuid(),
                WorkShiftId = request.WorkShiftId,
                BreakName = request.BreakName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DurationMinutes = duration,
                CreatedDate = TimeHelper.Now
            };

            breakRepo.Add(shiftBreak);
            _unitOfWork.Complete();

            return new ShiftBreakDto
            {
                Id = shiftBreak.Id,
                WorkShiftId = shiftBreak.WorkShiftId,
                BreakName = shiftBreak.BreakName,
                StartTime = shiftBreak.StartTime,
                EndTime = shiftBreak.EndTime,
                DurationMinutes = shiftBreak.DurationMinutes
            };
        }

        public ShiftBreakDto UpdateShiftBreak(Guid id, UpdateShiftBreakRequest request)
        {
            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();
            var shiftBreak = breakRepo.GetById(id);
            if (shiftBreak == null || shiftBreak.IsDeleted) return null;

            var duration = (decimal)(request.EndTime - request.StartTime).TotalMinutes;
            if (duration <= 0)
                throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu");

            shiftBreak.BreakName = request.BreakName;
            shiftBreak.StartTime = request.StartTime;
            shiftBreak.EndTime = request.EndTime;
            shiftBreak.DurationMinutes = duration;
            shiftBreak.UpdatedDate = TimeHelper.Now;

            breakRepo.Update(shiftBreak);
            _unitOfWork.Complete();

            return new ShiftBreakDto
            {
                Id = shiftBreak.Id,
                WorkShiftId = shiftBreak.WorkShiftId,
                BreakName = shiftBreak.BreakName,
                StartTime = shiftBreak.StartTime,
                EndTime = shiftBreak.EndTime,
                DurationMinutes = shiftBreak.DurationMinutes
            };
        }

        public bool DeleteShiftBreak(Guid id)
        {
            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();
            var shiftBreak = breakRepo.GetById(id);
            if (shiftBreak == null || shiftBreak.IsDeleted) return false;

            shiftBreak.IsDeleted = true;
            shiftBreak.DeletedDate = TimeHelper.Now;
            breakRepo.Update(shiftBreak);
            _unitOfWork.Complete();
            return true;
        }

        public List<ShiftBreakDto> GetShiftBreaks(Guid workShiftId)
        {
            var breakRepo = _unitOfWork.GetRepository<ShiftBreak>();
            return breakRepo.GetAll(b => b.WorkShiftId == workShiftId && !b.IsDeleted)
                .Select(b => new ShiftBreakDto
                {
                    Id = b.Id,
                    WorkShiftId = b.WorkShiftId,
                    BreakName = b.BreakName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    DurationMinutes = b.DurationMinutes
                }).ToList();
        }
    }
}