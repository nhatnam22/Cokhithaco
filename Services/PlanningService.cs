using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;
using CoKhiThaCo.Models.Dtos.MaterialFeedingPlan;
using CoKhiThaCo.Models.Dtos.Planning;
using CoKhiThaCo.Models.Dtos.ProductionCommand;
using CoKhiThaCo.Models.Enums;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace CoKhiThaCo.Services
{
    public interface IPlanningService
    {
        PagedResult<PlanningDto> GetPaged(PlanningListQuery query, ClaimsPrincipal currentUser);
        PlanningDto GetById(Guid id);
        PlanningDto Create(CreatePlanningRequest request);
        PlanningDto CreateAuto(CreateAutoPlanningRequest request);
        PlanningDto Schedule(Guid id, SchedulePlanningRequest request);
        PlanningDto Update(Guid id, UpdatePlanningRequest request);
        bool Delete(Guid id);
        void UpdateStatus(Guid id, PlanningStatus status);
        SummaryStatusPlan Summary();
        void ReAllocation(Guid id);
        ICollection<PlanningGroupMachine> GetPlanningGroupMachine(MachineListQuery query, DateTime date);
        List<PlanningDto> BatchUpdateSchedule(BatchUpdateScheduleRequest request);
        List<PlanningDto> BatchSchedule(BatchScheduleRequest request);
    }

    public class PlanningService : IPlanningService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionHelper _permissionHelper;
        private readonly IMachineService _machineService;
        private readonly IMaterialFeedingPlanService _materialFeedingPlanService;
        private readonly IProductionCommandService _productionCommandService;

        private static readonly TimeSpan WorkingDayStartTime = new TimeSpan(7, 30, 0);

        public PlanningService(IUnitOfWork unitOfWork, IPermissionHelper permissionHelper, IMachineService machineService, IMaterialFeedingPlanService materialFeedingPlanService, IProductionCommandService productionCommandService)
        {
            _unitOfWork = unitOfWork;
            _permissionHelper = permissionHelper;
            _machineService = machineService;
            _materialFeedingPlanService = materialFeedingPlanService;
            _productionCommandService = productionCommandService;
        }

        #region Read

        // [THAY ĐỔI] Include: "ProductionCommands" (collection) thay vì "ProductionCommand" (nav đơn)
        private readonly string[] _planningIncludes = new[]
        {
            "CommandCuttingFile.CuttingFile.CategoryCuttingFile",
            "CommandCuttingFile.ProductionCommands",
            "CommandCuttingFile.CuttingFile",
            "Machine",
            "Segments"
        };

        public PagedResult<PlanningDto> GetPaged(PlanningListQuery query, ClaimsPrincipal currentUser)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "OrderIndex", Direction = "asc" });

            //if (!_permissionHelper.IsAdmin(currentUser))
            //{
            //    var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);
            //    var operatorRepo = _unitOfWork.GetRepository<MachineOperator>();
            //    var myMachineIds = operatorRepo.GetAll(mo => mo.UserId == currentUserId && !mo.IsDeleted)
            //        .Select(mo => mo.MachineId)
            //        .ToHashSet();

            //    if (!myMachineIds.Any())
            //    {
            //        return new PagedResult<PlanningDto> { Items = new List<PlanningDto>(), Total = 0, Page = query.Page, PageSize = query.PageSize };
            //    }

            //    var items = repo.GetAllPaging(p => !p.IsDeleted && p.MachineId.HasValue && myMachineIds.Contains(p.MachineId.Value), out var total, query.Page, query.PageSize, sorts, filters, _planningIncludes);
            //    return BuildResult(items, total, query);
            //}

            if (query.MachineId.HasValue)
            {
                var machineId = query.MachineId.Value;
                var items = repo.GetAllPaging(p => !p.IsDeleted && p.MachineId == machineId, out var total2, query.Page, query.PageSize, sorts, filters, _planningIncludes);
                return BuildResult(items, total2, query);
            }

            var allItems = repo.GetAllPaging(p => !p.IsDeleted 
            && (string.IsNullOrEmpty(query.Keyword) || p.CommandCuttingFile.ProductionCommands.Select(x => x.CommandCode).Contains(query.Keyword) || p.CommandCuttingFile.CuttingFile.Code.Contains(query.Keyword)), out var total3, query.Page, query.PageSize, sorts, filters, _planningIncludes);
            return BuildResult(allItems, total3, query);
        }

        public PlanningDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var planning = repo.GetSingle(p => p.Id == id && !p.IsDeleted, includes: _planningIncludes);
            return planning == null ? null : MapToDto(planning);
        }

        #endregion

        #region Create

        public PlanningDto Create(CreatePlanningRequest request)
        {
            var ccf = ValidateCommandCuttingFile(request.CommandCuttingFileId);
            ValidateMachine(ccf.CategoryCuttingFileId, request.MachineId);

            var repo = _unitOfWork.GetRepository<Planning>();
            if (repo.Exists(p => p.CommandCuttingFileId == request.CommandCuttingFileId && !p.IsDeleted))
                throw new Exception("Lệnh file cắt này đã được lập kế hoạch");

            var planning = new Planning
            {
                Id = Guid.NewGuid(),
                CommandCuttingFileId = request.CommandCuttingFileId,
                MachineId = request.MachineId,
                OrderIndex = request.OrderIndex,
                Status = PlanningStatus.Pending,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(planning);
            _unitOfWork.Complete();

            UpdateCommandCuttingFilePlannedQuantity(ccf.Id, ccf.Quantity);
            return GetById(planning.Id);
        }

        public PlanningDto CreateAuto(CreateAutoPlanningRequest request)
        {
            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccf = ccfRepo.GetSingle(c => c.Id == request.CommandCuttingFileId && !c.IsDeleted);
            if (ccf == null) throw new Exception("Không tìm thấy lệnh file cắt");

            var repo = _unitOfWork.GetRepository<Planning>();
            if (repo.Exists(p => p.CommandCuttingFileId == request.CommandCuttingFileId && !p.IsDeleted))
                throw new Exception("Lệnh file cắt này đã được lập kế hoạch");

            var planning = new Planning
            {
                Id = Guid.NewGuid(),
                CommandCuttingFileId = request.CommandCuttingFileId,
                MachineId = null,
                OrderIndex = 0,
                Status = PlanningStatus.Pending,
                StartTime = DateTime.MinValue,
                EndTime = DateTime.MinValue,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(planning);
            _unitOfWork.Complete();
            return GetById(planning.Id);
        }

        #endregion

        #region Schedule

        public PlanningDto Schedule(Guid id, SchedulePlanningRequest request)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var planning = repo.GetById(id);
            if (planning == null || planning.IsDeleted) return null;
            if (planning.Status != PlanningStatus.Pending)
                throw new Exception("Chỉ có thể lên kế hoạch khi đang ở trạng thái chờ");

            var ccf = ValidateCommandCuttingFile(planning.CommandCuttingFileId);
            ValidateMachine(ccf.CategoryCuttingFileId, request.MachineId);

            var workingDayStart = GetWorkingDayStart(request.Date);
            var intervals = GetMachineWorkingDayIntervals(request.MachineId, workingDayStart);
            var totalWorkingMinutes = intervals.Sum(i => (decimal)(i.End - i.Start).TotalMinutes);
            if (ccf.CuttingTime > totalWorkingMinutes)
                throw new Exception($"Thờigian cắt ({ccf.CuttingTime} phút) vượt quá tổng thờigian làm việc của máy trong ngày ({totalWorkingMinutes} phút)");

            var proposedStart = FindAppendStart(request.MachineId, workingDayStart, intervals, id);
            var segments = GenerateSegments(proposedStart, ccf.CuttingTime, intervals);

            planning.MachineId = request.MachineId;
            planning.StartTime = segments.First().StartTime;
            planning.EndTime = segments.Last().EndTime;
            planning.OrderIndex = CalculateOrderIndex(request.MachineId, workingDayStart, planning.StartTime, id);
            planning.Status = PlanningStatus.InProgress;
            planning.UpdatedDate = TimeHelper.Now;

            repo.Update(planning);
            SaveSegments(planning.Id, segments);
            _unitOfWork.Complete();

            UpdateCommandCuttingFilePlannedQuantity(ccf.Id, ccf.Quantity);
            return GetById(id);
        }

        #endregion

        #region Update

        public PlanningDto Update(Guid id, UpdatePlanningRequest request)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var planning = repo.GetById(id);
            if (planning == null || planning.IsDeleted) return null;
            if (planning.Status != PlanningStatus.InProgress)
                throw new Exception("Chỉ có thể cập nhật kế hoạch đang xử lý");

            var ccf = ValidateCommandCuttingFile(planning.CommandCuttingFileId);
            ValidateMachine(ccf.CategoryCuttingFileId, request.MachineId);

            var workingDayStart = GetWorkingDayStart(request.Date);
            var intervals = GetMachineWorkingDayIntervals(request.MachineId, workingDayStart);

            List<PlanningSegment> segments;

            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                if (request.StartTime.Value < workingDayStart || request.EndTime.Value > workingDayStart.AddDays(1))
                    throw new Exception("Thờigian phải nằm trong ngày làm việc (7h30 hôm nay đến 7h30 hôm sau)");

                segments = CalculateSegmentsFromRange(request.StartTime.Value, request.EndTime.Value, intervals);
                var actualMinutes = segments.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalMinutes);
                if (actualMinutes < ccf.CuttingTime)
                    throw new Exception($"Thờigian làm việc thực tế ({actualMinutes} phút) không đủ cho thờigian cắt ({ccf.CuttingTime} phút)");

                if (!_machineService.IsMachineAvailable(request.MachineId, request.StartTime.Value, request.EndTime.Value, id))
                    throw new Exception("Máy không rảnh trong khoảng thờigian này");
            }
            else
            {
                var proposedStart = FindAppendStart(request.MachineId, workingDayStart, intervals, id);
                segments = GenerateSegments(proposedStart, ccf.CuttingTime, intervals);
            }

            planning.MachineId = request.MachineId;
            planning.StartTime = segments.First().StartTime;
            planning.EndTime = segments.Last().EndTime;
            planning.OrderIndex = CalculateOrderIndex(request.MachineId, workingDayStart, planning.StartTime, id);
            planning.UpdatedDate = TimeHelper.Now;

            repo.Update(planning);
            SaveSegments(planning.Id, segments);
            _unitOfWork.Complete();

            return GetById(id);
        }

        #endregion

        #region Delete

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var planning = repo.GetById(id);
            if (planning == null || planning.IsDeleted) return false;

            if (planning.Status == PlanningStatus.Completed)
            {
                planning.Status = PlanningStatus.Cancelled;
                planning.UpdatedDate = TimeHelper.Now;
                CancelRelatedMaterialFeedingPlans(id);
            }

            planning.IsDeleted = true;
            planning.DeletedDate = TimeHelper.Now;
            repo.Update(planning);

            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccf = ccfRepo.GetById(planning.CommandCuttingFileId);
            if (ccf != null && !ccf.IsDeleted)
            {
                ccf.PlannedQuantity = Math.Max(0, ccf.PlannedQuantity - ccf.Quantity);
                if (ccf.PlannedQuantity == 0 && ccf.Status == ProductionOrderStatus.Planned)
                    ccf.Status = ProductionOrderStatus.Created;
                ccf.UpdatedDate = TimeHelper.Now;
                ccfRepo.Update(ccf);
            }

            _unitOfWork.Complete();
            return true;
        }

        #endregion

        #region Status

        public void UpdateStatus(Guid id, PlanningStatus status)
        {
            var repo = _unitOfWork.GetRepository<Planning>();
            var planning = repo.GetById(id);
            if (planning == null || planning.IsDeleted) throw new Exception("Kế hoạch không tồn tại");

            if (planning.Status == PlanningStatus.Completed)
                throw new Exception("Kế hoạch đã hoàn thành, không thể thay đổi trạng thái");
            if (planning.Status == PlanningStatus.Done)
                throw new Exception("Kế hoạch đã thực thi xong, không thể thay đổi trạng thái");
            //if (status != PlanningStatus.Completed)
            //    throw new Exception("Trạng thái không hợp lệ");

            if (status == PlanningStatus.Completed && planning.MachineId == null)
                throw new Exception("Trạng thái không hợp lệ");
            if (status != PlanningStatus.Cancelled && status != PlanningStatus.Completed)
                throw new Exception("Trạng thái không hợp lệ");

            planning.Status = status;
            planning.UpdatedDate = TimeHelper.Now;
            repo.Update(planning);
            _unitOfWork.Complete();

            if (status == PlanningStatus.Completed)
            {
                var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
                var ccf = ccfRepo.GetById(planning.CommandCuttingFileId);
                if (ccf != null && !ccf.IsDeleted)
                {
                    _materialFeedingPlanService.Create(new CreateMaterialFeedingPlanRequest
                    {
                        PlanningId = planning.Id,
                        Quantity = ccf.Quantity,
                        //Length = ccf.Length,
                        //Width = ccf.Width,
                        //Thickness = ccf.Thickness,
                        IsDirector = true
                    });

                    var executeRepo = _unitOfWork.GetRepository<Execute>();
                    var existingExecute = executeRepo.GetSingle(e => e.PlanningId == planning.Id && !e.IsDeleted);
                    if (existingExecute == null)
                    {
                        var execute = new Execute
                        {
                            Id = Guid.NewGuid(),
                            PlanningId = planning.Id,
                            Status = ExecuteStatus.UnCompleted,
                            ReceiveStatus = ReceiveMaterialFeedStatus.NotReceive,
                            CreatedDate = TimeHelper.Now
                        };
                        executeRepo.Add(execute);
                        _unitOfWork.Complete();
                    }
                }
            }
        }

        public SummaryStatusPlan Summary()
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var allPlan = planningRepo.GetAll(p => !p.IsDeleted).ToList();
            return new SummaryStatusPlan
            {
                TotalPendingStatus = allPlan.Count(x => x.Status == PlanningStatus.Pending),
                TotalProcessStatus = allPlan.Count(x => x.Status == PlanningStatus.InProgress),
                TotalCompletedStatus = allPlan.Count(x => x.Status == PlanningStatus.Completed),
                TotalCancelStatus = allPlan.Count(x => x.Status == PlanningStatus.Cancelled),
                TotalDone = allPlan.Count(x => x.Status == PlanningStatus.Done)
            };
        }
        public void ReAllocation (Guid id)
        {
            var planRepo = _unitOfWork.GetRepository<Planning>();
            var currentPlan = planRepo.GetSingle(p => p.Id == id && !p.IsDeleted);
            if (currentPlan == null) throw new Exception("kế hoạch không tồn tại");
            currentPlan.MachineId = null;
            currentPlan.StartTime = DateTime.MinValue;
            currentPlan.EndTime = DateTime.MaxValue;
            currentPlan.OrderIndex = 0;
            currentPlan.Status = PlanningStatus.Pending;
            planRepo.Update(currentPlan);
            _unitOfWork.Complete();
        }
        public ICollection<PlanningGroupMachine> GetPlanningGroupMachine(MachineListQuery query, DateTime date)
        {
            Guid? machineId = null;
            if (!string.IsNullOrEmpty(query.Keyword) && Guid.TryParse(query.Keyword, out Guid parsedGuid))
            {
                machineId = parsedGuid;
            }

            var workingTime = new TimeSpan(7, 30, 0);
            var workingDate = date.Date.Add(workingTime);
            var endWorkingDate = workingDate.AddDays(1);


            var planRepo = _unitOfWork.GetRepository<Planning>();
            var plans = planRepo.GetAll(
                whereCondition: p => !p.IsDeleted
                && (p.StartTime < endWorkingDate && p.EndTime > workingDate)
                && (!machineId.HasValue || p.MachineId == machineId.Value),
                sorts: SortHelper.ParseSorts("StartTime:asc"),
                includes: _planningIncludes)
                .Where(p => p.MachineId.HasValue)
                .ToList();

            var categoryMachineIds = plans
                .Where(p => p.CommandCuttingFile != null)
                .Select(p => p.CommandCuttingFile.CategoryCuttingFileId)
                .Distinct()
                .ToList();


            var planGroupByMachine = plans
                .GroupBy(p => p.MachineId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var activeTimes = plans
                .GroupBy(p => p.MachineId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => (x.EndTime - x.StartTime).TotalMinutes));


            var machineCategoryRepo = _unitOfWork.GetRepository<MachineCategoryCuttingFile>();
            var machines = machineCategoryRepo.GetAll(
                m => categoryMachineIds.Contains(m.CategoryCuttingFileId) && !m.IsDeleted,
                includes: new[] { "Machine" })
                .Where(m => m.Machine != null)
                .GroupBy(g => g.MachineId)
                .ToDictionary(g => g.Key, g => g.First().Machine);

            var machineWorkShiftRepo = _unitOfWork.GetRepository<WorkShiftMachine>();
            var machinesWorkshifts = machineWorkShiftRepo.GetAll(
                mw => machines.Keys.Contains(mw.MachineId) && !mw.IsDeleted,
                includes: new[] { "WorkShift" })
                .Where(mw => mw.WorkShift != null)
                .GroupBy(mw => mw.MachineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.WorkShift.StandardTimeMinutes));
            var result = new List<PlanningGroupMachine>();
            foreach (var machineKvp in machines)
            {
                var machineIdKey = machineKvp.Key;
                var machineEntity = machineKvp.Value;
                var standardTime = machinesWorkshifts.GetValueOrDefault(machineIdKey, 0m);
                var activeTime = activeTimes.GetValueOrDefault(machineIdKey, 0d);

                var item = new PlanningGroupMachine
                {
                    MachineId = machineEntity.MachineId,
                    MachineCode = machineEntity.MachineCode,
                    MachineName = machineEntity.MachineName,
                    Status = (int)machineEntity.Status,
                    IsReserveMachine = machineEntity.IsReserveMachine,
                    PowerKw = machineEntity.PowerKw,
                    TimeRemain = standardTime - (decimal)activeTime,
                    Plans = new List<PlanningDto>()
                };

                if (planGroupByMachine.TryGetValue(machineIdKey, out var machinePlans))
                {
                    item.Plans = machinePlans.Select(MapToDto).ToList();
                }

                result.Add(item);
            }

            return result;
        }

        public List<PlanningDto> BatchUpdateSchedule(BatchUpdateScheduleRequest request)
        {
            if (request.Items == null || !request.Items.Any())
                throw new Exception("Danh sách cập nhật không được rỗng");

            var repo = _unitOfWork.GetRepository<Planning>();
            var ids = request.Items.Select(i => i.Id).ToList();
            var plannings = repo.GetAll(p => ids.Contains(p.Id) && !p.IsDeleted, includes: _planningIncludes).ToList();

            if (plannings.Count != ids.Count)
                throw new Exception("Một hoặc nhiều kế hoạch không tồn tại");

            foreach (var p in plannings)
            {
                if (p.Status != PlanningStatus.InProgress)
                    throw new Exception($"Kế hoạch {p.Id} không ở trạng thái đang xử lý");
                if (!p.MachineId.HasValue)
                    throw new Exception($"Kế hoạch {p.Id} chưa được phân công máy");
            }

            var machineId = plannings.First().MachineId.Value;
            if (plannings.Any(p => p.MachineId.Value != machineId))
                throw new Exception("Tất cả kế hoạch phải thuộc cùng một máy");

            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccfIds = plannings.Select(p => p.CommandCuttingFileId).Distinct().ToList();
            var ccfs = ccfRepo.GetAll(c => ccfIds.Contains(c.Id) && !c.IsDeleted).ToDictionary(c => c.Id);

            var workingDayStart = GetWorkingDayStart(request.Items.First().StartTime);
            var intervals = GetMachineWorkingDayIntervals(machineId, workingDayStart);

            var orderedItems = request.Items.ToList();
            var result = new List<PlanningDto>();
            DateTime? currentEnd = null;

            foreach (var item in orderedItems)
            {
                var planning = plannings.First(p => p.Id == item.Id);
                var ccf = ccfs.GetValueOrDefault(planning.CommandCuttingFileId);
                if (ccf == null)
                    throw new Exception($"Không tìm thấy lệnh file cắt cho kế hoạch {planning.Id}");

                DateTime start;
                if (!currentEnd.HasValue)
                {
                    start = item.StartTime;
                }
                else
                {
                    start = AdvanceToWorkingInterval(currentEnd.Value, intervals);
                }

                var segments = GenerateSegments(start, ccf.CuttingTime, intervals);

                planning.StartTime = segments.First().StartTime;
                planning.EndTime = segments.Last().EndTime;
                planning.OrderIndex = CalculateOrderIndex(machineId, workingDayStart, planning.StartTime, planning.Id);
                planning.UpdatedDate = TimeHelper.Now;

                repo.Update(planning);
                SaveSegments(planning.Id, segments);
                currentEnd = planning.EndTime;
            }

            _unitOfWork.Complete();

            foreach (var id in ids)
            {
                result.Add(GetById(id));
            }
            return result;
        }

        public List<PlanningDto> BatchSchedule(BatchScheduleRequest request)
        {
            if (request.PlanningIds == null || !request.PlanningIds.Any())
                throw new Exception("Danh sách kế hoạch không được rỗng");

            var repo = _unitOfWork.GetRepository<Planning>();
            var plannings = repo.GetAll(
                p => request.PlanningIds.Contains(p.Id) && !p.IsDeleted,
                includes: _planningIncludes).ToList();

            if (plannings.Count != request.PlanningIds.Count)
                throw new Exception("Một hoặc nhiều kế hoạch không tồn tại");

            foreach (var p in plannings)
            {
                if (p.Status != PlanningStatus.Pending)
                    throw new Exception($"Kế hoạch {p.Id} không ở trạng thái chờ");
            }

            var machineId = request.MachineId;
            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccfIds = plannings.Select(p => p.CommandCuttingFileId).Distinct().ToList();
            var ccfs = ccfRepo.GetAll(c => ccfIds.Contains(c.Id) && !c.IsDeleted).ToDictionary(c => c.Id);

            foreach (var p in plannings)
            {
                var ccf = ccfs.GetValueOrDefault(p.CommandCuttingFileId);
                if (ccf == null)
                    throw new Exception($"Không tìm thấy lệnh file cắt cho kế hoạch {p.Id}");
                ValidateMachine(ccf.CategoryCuttingFileId, machineId);
            }

            var workingDayStart = GetWorkingDayStart(request.Date);
            var intervals = GetMachineWorkingDayIntervals(machineId, workingDayStart);

            var totalWorkingMinutes = intervals.Sum(i => (decimal)(i.End - i.Start).TotalMinutes);
            var totalCuttingTime = plannings.Sum(p => ccfs.GetValueOrDefault(p.CommandCuttingFileId)?.CuttingTime ?? 0);
            if (totalCuttingTime > totalWorkingMinutes)
                throw new Exception($"Tổng thời gian cắt ({totalCuttingTime} phút) vượt quá tổng thời gian làm việc của máy trong ngày ({totalWorkingMinutes} phút)");

            var orderedPlannings = request.PlanningIds
                .Select(id => plannings.First(p => p.Id == id))
                .ToList();

            var result = new List<PlanningDto>();
            DateTime? currentEnd = null;
            var orderIndex = 0;

            foreach (var planning in orderedPlannings)
            {
                var ccf = ccfs.GetValueOrDefault(planning.CommandCuttingFileId);

                DateTime start;
                if (!currentEnd.HasValue)
                {
                    start = FindAppendStart(machineId, workingDayStart, intervals, null);
                }
                else
                {
                    start = AdvanceToWorkingInterval(currentEnd.Value, intervals);
                }

                var segments = GenerateSegments(start, ccf.CuttingTime, intervals);

                planning.MachineId = machineId;
                planning.StartTime = segments.First().StartTime;
                planning.EndTime = segments.Last().EndTime;
                planning.OrderIndex = ++orderIndex;
                planning.Status = PlanningStatus.InProgress;
                planning.UpdatedDate = TimeHelper.Now;

                repo.Update(planning);
                SaveSegments(planning.Id, segments);
                currentEnd = planning.EndTime;
            }

            _unitOfWork.Complete();

            foreach (var ccfId in ccfIds)
            {
                var ccf = ccfs.GetValueOrDefault(ccfId);
                if (ccf != null)
                {
                    UpdateCommandCuttingFilePlannedQuantity(ccf.Id, ccf.Quantity);
                }
            }

            foreach (var id in request.PlanningIds)
            {
                result.Add(GetById(id));
            }
            return result;
        }

        #endregion

        #region Helpers

        private DateTime GetWorkingDayStart(DateTime date)
        {
            return date.Date + WorkingDayStartTime;
        }

        private CommandCuttingFile ValidateCommandCuttingFile(Guid commandCuttingFileId)
        {
            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccf = ccfRepo.GetSingle(c => c.Id == commandCuttingFileId && !c.IsDeleted,
                includes: new[] { "CuttingFile.CategoryCuttingFile", "ProductionCommands" });
            if (ccf == null) throw new Exception("Không tìm thấy lệnh file cắt");
            if (ccf.Status == ProductionOrderStatus.Completed || ccf.Status == ProductionOrderStatus.Cancelled)
                throw new Exception("Không thể lập kế hoạch cho mục đã hoàn thành hoặc đã hủy");
            return ccf;
        }

        private void ValidateMachine(Guid? categoryCuttingFileId, Guid machineId)
        {
            var machineRepo = _unitOfWork.GetRepository<Machine>();
            var machine = machineRepo.GetSingle(m => m.MachineId == machineId && !m.IsDeleted);
            if (machine == null) throw new Exception("Máy không tồn tại");
            if (machine.Status != MachineStatus.Active)
                throw new Exception("Máy không hoạt động");

            if (categoryCuttingFileId.HasValue && !_machineService.CanHandleCategory(machineId, categoryCuttingFileId.Value))
                throw new Exception("Máy này không hỗ trợ loại file cắt này");
        }

        private List<(DateTime Start, DateTime End)> GetMachineWorkingDayIntervals(Guid machineId, DateTime workingDayStart)
        {
            var workingDayEnd = workingDayStart.AddDays(1);
            var wsmList = _unitOfWork.GetRepository<WorkShiftMachine>()
                .GetAll(wsm => wsm.MachineId == machineId && !wsm.IsDeleted, includes: new[] { "WorkShift" })
                .ToList();

            if (!wsmList.Any())
                throw new Exception("Máy này chưa được phân công ca làm việc");

            var intervals = new List<(DateTime Start, DateTime End)>();
            var baseDate = workingDayStart.Date;

            foreach (var wsm in wsmList)
            {
                var shift = wsm.WorkShift;
                var shiftStart = baseDate + shift.StartTime;
                var shiftEnd = baseDate + shift.EndTime;
                if (shiftEnd <= shiftStart)
                    shiftEnd = shiftEnd.AddDays(1);

                var breaks = _unitOfWork.GetRepository<ShiftBreak>()
                    .GetAll(b => b.WorkShiftId == shift.Id && !b.IsDeleted)
                    .Select(b =>
                    {
                        var bStart = baseDate + b.StartTime;
                        var bEnd = baseDate + b.EndTime;
                        if (bEnd <= bStart)
                            bEnd = bEnd.AddDays(1);
                        return (Start: bStart, End: bEnd);
                    })
                    .Where(b => b.End > shiftStart && b.Start < shiftEnd)
                    .OrderBy(b => b.Start)
                    .ToList();

                var current = shiftStart;
                foreach (var breakItem in breaks)
                {
                    if (breakItem.Start > current)
                        intervals.Add((current, breakItem.Start));
                    if (breakItem.End > current)
                        current = breakItem.End;
                }
                if (current < shiftEnd)
                    intervals.Add((current, shiftEnd));
            }

            return intervals
                .Select(i => (
                    Start: i.Start < workingDayStart ? workingDayStart : i.Start,
                    End: i.End > workingDayEnd ? workingDayEnd : i.End
                ))
                .Where(i => i.End > i.Start)
                .OrderBy(i => i.Start)
                .ToList();
        }

        private DateTime FindAppendStart(Guid machineId, DateTime workingDayStart, List<(DateTime Start, DateTime End)> intervals, Guid? excludePlanningId)
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var workingDayEnd = workingDayStart.AddDays(1);
            var existingPlannings = planningRepo.GetAll(
                p => p.MachineId == machineId && !p.IsDeleted && p.Status != PlanningStatus.Cancelled && p.Id != excludePlanningId)
                .Where(p => p.StartTime < workingDayEnd && p.EndTime > workingDayStart)
                .OrderBy(p => p.EndTime)
                .ToList();

            if (!existingPlannings.Any())
                return intervals.First().Start;

            return AdvanceToWorkingInterval(existingPlannings.Last().EndTime, intervals);
        }

        private DateTime AdvanceToWorkingInterval(DateTime time, List<(DateTime Start, DateTime End)> intervals)
        {
            var containing = intervals.FirstOrDefault(i => i.Start <= time && i.End > time);
            if (containing != default) return time;

            var next = intervals.FirstOrDefault(i => i.Start > time);
            if (next == default)
                throw new Exception("Không còn khoảng làm việc nào trong ngày để nối đuôi");

            return next.Start;
        }

        private List<PlanningSegment> GenerateSegments(DateTime startTime, decimal cuttingMinutes, List<(DateTime Start, DateTime End)> intervals)
        {
            var segments = new List<PlanningSegment>();
            var current = startTime;
            var remaining = cuttingMinutes;
            var orderIndex = 1;
            var workingDayEnd = intervals.Last().End;

            while (remaining > 0)
            {
                var interval = intervals.FirstOrDefault(i => i.Start <= current && i.End > current);
                if (interval == default)
                {
                    interval = intervals.FirstOrDefault(i => i.Start >= current);
                    if (interval == default || interval.Start >= workingDayEnd)
                        throw new Exception("Không đủ thờigian làm việc trong ngày");
                    current = interval.Start;
                }

                var availableMinutes = (decimal)(interval.End - current).TotalMinutes;
                if (remaining <= availableMinutes)
                {
                    segments.Add(new PlanningSegment
                    {
                        Id = Guid.NewGuid(),
                        OrderIndex = orderIndex++,
                        StartTime = current,
                        EndTime = current.AddMinutes((double)remaining)
                    });
                    remaining = 0;
                }
                else
                {
                    segments.Add(new PlanningSegment
                    {
                        Id = Guid.NewGuid(),
                        OrderIndex = orderIndex++,
                        StartTime = current,
                        EndTime = interval.End
                    });
                    remaining -= availableMinutes;
                    current = interval.End;
                }
            }

            return segments;
        }

        private List<PlanningSegment> CalculateSegmentsFromRange(DateTime startTime, DateTime endTime, List<(DateTime Start, DateTime End)> intervals)
        {
            var segments = new List<PlanningSegment>();
            var orderIndex = 1;

            foreach (var interval in intervals)
            {
                var segStart = interval.Start > startTime ? interval.Start : startTime;
                var segEnd = interval.End < endTime ? interval.End : endTime;
                if (segEnd > segStart)
                {
                    segments.Add(new PlanningSegment
                    {
                        Id = Guid.NewGuid(),
                        OrderIndex = orderIndex++,
                        StartTime = segStart,
                        EndTime = segEnd
                    });
                }
            }

            return segments;
        }

        private void SaveSegments(Guid planningId, List<PlanningSegment> segments)
        {
            var segmentRepo = _unitOfWork.GetRepository<PlanningSegment>();
            var existingSegments = segmentRepo.GetAll(s => s.PlanningId == planningId).ToList();
            foreach (var existing in existingSegments)
            {
                segmentRepo.Delete(existing.Id);
            }

            foreach (var segment in segments)
            {
                segment.PlanningId = planningId;
                segmentRepo.Add(segment);
            }
        }

        private void UpdateCommandCuttingFilePlannedQuantity(Guid commandCuttingFileId, int quantity)
        {
            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccf = ccfRepo.GetById(commandCuttingFileId);
            if (ccf == null || ccf.IsDeleted) return;

            ccf.PlannedQuantity = Math.Max(ccf.PlannedQuantity, quantity);
            if (ccf.Status == ProductionOrderStatus.Created)
                ccf.Status = ProductionOrderStatus.Planned;
            ccf.UpdatedDate = TimeHelper.Now;
            ccfRepo.Update(ccf);
            _unitOfWork.Complete();
        }

        private void CancelRelatedMaterialFeedingPlans(Guid planningId)
        {
            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var items = repo.GetAll(mf => mf.PlanningId == planningId && !mf.IsDeleted).ToList();
            foreach (var item in items)
            {
                item.Status = MaterialFeeding.Cancelled;
                item.UpdatedDate = TimeHelper.Now;
                repo.Update(item);
            }
        }

        private int CalculateOrderIndex(Guid machineId, DateTime workingDayStart, DateTime startTime, Guid? excludePlanningId)
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var workingDayEnd = workingDayStart.AddDays(1);
            var existingCount = planningRepo.GetAll(
                p => p.MachineId == machineId && !p.IsDeleted && p.Status != PlanningStatus.Cancelled && p.Id != excludePlanningId)
                .Count(p => p.StartTime < workingDayEnd && p.EndTime > workingDayStart && p.StartTime < startTime);
            return existingCount + 1;
        }

        #endregion

        #region Mapping

        private PagedResult<PlanningDto> BuildResult(ICollection<Planning> items, int total, PlanningListQuery query)
        {
            return new PagedResult<PlanningDto>
            {
                Items = items.Select(MapToDto).ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        private PlanningDto MapToDto(Planning p)
        {
            return new PlanningDto
            {
                Id = p.Id,
                CommandCuttingFileId = p.CommandCuttingFileId,
                FileCode = p.CommandCuttingFile?.CuttingFile?.Code,
                FileName = p.CommandCuttingFile?.CuttingFile?.Name,
                // [THAY ĐỔI] Lấy ProductionCommandCode/OrderName từ collection (FirstOrDefault vì 1 CCF có nhiều PC)
                ProductionCommandCodes = p.CommandCuttingFile?.ProductionCommands?.Select(x => new ProductionCommandDto()
                {
                    Id = x.Id,
                    CommandCode = x.CommandCode,
                    OrderName = x.OrderName,
                }).ToList(),
                OrderName = p.CommandCuttingFile?.ProductionCommands?.FirstOrDefault()?.OrderName,
                MachineId = p.MachineId,
                MachineCode = p.Machine?.MachineCode,
                MachineName = p.Machine?.MachineName,
                OrderIndex = p.OrderIndex,
                Status = (int)p.Status,
                StatusName = p.Status.ToString(),
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                Quantity = p.CommandCuttingFile?.Quantity ?? 0,
                CuttingTime = p.CommandCuttingFile?.CuttingTime ?? 0,
                Thickness = p.CommandCuttingFile?.Thickness ?? 0,
                CategoryCuttingFileCode = p.CommandCuttingFile?.CategoryCuttingFile?.Code,
                Segments = p.Segments?.OrderBy(s => s.OrderIndex).Select(s => new PlanningSegmentDto
                {
                    Id = s.Id,
                    PlanningId = s.PlanningId,
                    OrderIndex = s.OrderIndex,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList() ?? new List<PlanningSegmentDto>()
            };
        }

        #endregion
    }
}