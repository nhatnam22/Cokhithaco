using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Execute;
using CoKhiThaCo.Models.Enums;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.Pkcs;

namespace CoKhiThaCo.Services
{
    public interface IExecuteService
    {
        PagedResult<ExecuteDto> GetPaged(ExecuteListQuery query, ClaimsPrincipal currentUser);
        ExecuteDto GetById(Guid id);
        ExecuteDto Start(StartExecuteRequest request, ClaimsPrincipal currentUser);
        ExecuteDto Complete(Guid id, CompleteExecuteRequest request);
        ExecuteDto Cancel(Guid id, CancelExecuteRequest request);
        SummaryStatusExecute Summary();
        ExecuteDto ReceiveMaterialFeed(Guid executeId, ComfirmReceiveRequest request);
        SubExecuteDto StartSubExecute(StartSubExecuteRequest request, ClaimsPrincipal currentUser);
        SubExecuteDto EndSubExecute(Guid executeId, EndSubExecuteRequest request, ClaimsPrincipal currentUser);
        SubExecuteDto ContinuteSubExecute(Guid executeId, ClaimsPrincipal currentUser);
        void Completed(Guid executeId);
    }

    public class ExecuteService : IExecuteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionHelper _permissionHelper;
        private readonly IMachineService _machineService;
        private readonly ICommandCuttingFileService _commandCuttingFileService;
        private readonly IMaterialFeedingPlanService _materialFeedingPlanService;

        public ExecuteService(
            IUnitOfWork unitOfWork,
            IPermissionHelper permissionHelper,
            IMachineService machineService,
            ICommandCuttingFileService commandCuttingFileService,
            IMaterialFeedingPlanService materialFeedingPlanService)
        {
            _unitOfWork = unitOfWork;
            _permissionHelper = permissionHelper;
            _machineService = machineService;
            _commandCuttingFileService = commandCuttingFileService;
            _materialFeedingPlanService = materialFeedingPlanService;
        }

        private const string IncludePlanning = "Planning.CommandCuttingFile.CuttingFile.CategoryCuttingFile";
        // [THAY ĐỔI] Include: "ProductionCommands" (collection) thay vì "ProductionCommand" (nav đơn)
        private const string IncludePlanningCommand = "Planning.CommandCuttingFile.ProductionCommands";
        private const string IncludePlanningMachine = "Planning.Machine";
        private const string IncludeUser = "User";

        #region Read

        public PagedResult<ExecuteDto> GetPaged(ExecuteListQuery query, ClaimsPrincipal currentUser)
        {
            var repo = _unitOfWork.GetRepository<Execute>();
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "StartTime", Direction = "desc" });

            var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);
            var isAdmin = _permissionHelper.IsAdmin(currentUser);
            var isManageMaterial = _permissionHelper.IsManageMaterial(currentUser);

            var items = repo.GetAll(
                e => !e.IsDeleted,
                includes: new[] { "Planning.CommandCuttingFile", IncludePlanning, IncludePlanningCommand, IncludePlanningMachine, IncludeUser });

            if (!isAdmin && !isManageMaterial)
            {
                var operatorRepo = _unitOfWork.GetRepository<MachineOperator>();
                var myMachineIds = operatorRepo.GetAll(mo => mo.UserId == currentUserId && !mo.IsDeleted)
                    .Select(mo => mo.MachineId)
                    .ToHashSet();

                items = items.Where(e => (e.Planning.MachineId.HasValue && myMachineIds.Contains(e.Planning.MachineId.Value)) || e.UserId == currentUserId).ToList();
            }
            else if (query.MachineId.HasValue)
            {
                items = items.Where(e => e.Planning.MachineId == query.MachineId.Value).ToList();
            }

            var total = items.Count;
            var paged = items
                .OrderByDescending(e => e.StartTime)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<ExecuteDto>
            {
                Items = paged.Select(MapToDto).ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public ExecuteDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Execute>();
            var execute = repo.GetSingle(e => e.Id == id && !e.IsDeleted,
                includes: new[] { IncludePlanning, IncludePlanningCommand, IncludePlanningMachine, IncludeUser });
            return execute == null ? null : MapToDto(execute);
        }

        #endregion

        #region ReceiveMaterialFeed
        // Xác nhận đã nhận bao nhiêu phôi cho lệnh thực thi nào, nhận phôi từ lệnh cấp phôi nào
        public ExecuteDto ReceiveMaterialFeed(Guid executeId, ComfirmReceiveRequest request)
        {
            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var execute = executeRepo.GetSingle(e => e.Id == executeId && !e.IsDeleted,
                includes: new[] { IncludePlanning, IncludePlanningMachine, IncludeUser });
            if (execute == null) throw new Exception("Lệnh thực thi không tồn tại");
            // Check hoàn thành
            if (execute.Status == ExecuteStatus.Completed)
                throw new Exception("Lệnh thực thi đã hoàn thành");
            // Check lệnh thực thi đã nhận đủ phôi chưa
            if (execute.ReceiveStatus == ReceiveMaterialFeedStatus.Enough)
                throw new Exception("Lệnh thực thi đã được xác nhận nhận phôi");
            // Check kế hoạch cấp phôi có tồn tại không
            var mfpRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var mfp = mfpRepo.GetSingle(m => m.PlanningId == execute.PlanningId && !m.IsDeleted && m.IsDirector);
            if (mfp == null) throw new Exception("Kế hoạch cấp phôi không tồn tại");
            var receiveRepo = _unitOfWork.GetRepository<ReceiveMaterialFeed>();
            var receives = receiveRepo.GetAll(r => r.ExecuteId == execute.Id).Sum(r => r.QuantityConfirmReceived);

            if (request.QuantityConfirm <= 0)
                throw new Exception("Số lượng xác nhận phải lớn hơn 0");

            if (request.QuantityConfirm > mfp.QuantityActual - receives)
                throw new Exception($"Số lượng xác nhận ({request.QuantityConfirm}) vượt quá số lượng đã cấp ({mfp.QuantityActual - receives})");
            // Tạo record lưu thời điểm xác nhận
            var receive = new ReceiveMaterialFeed
            {
                Id = Guid.NewGuid(),
                ExecuteId = executeId,
                QuantityConfirmReceived = request.QuantityConfirm,
                ReceivedTime = TimeHelper.Now,
                CreatedDate = TimeHelper.Now
            };
            receiveRepo.Add(receive);

            var totalReceived = receiveRepo.GetAll(r => r.ExecuteId == executeId && !r.IsDeleted).Sum(r => r.QuantityConfirmReceived) + request.QuantityConfirm;
            var requiredQuantity = execute.Planning?.CommandCuttingFile?.Quantity ?? 0;

            if (totalReceived >= requiredQuantity)
            {
                execute.ReceiveStatus = ReceiveMaterialFeedStatus.Enough;
            }
            else
            {
                execute.ReceiveStatus = ReceiveMaterialFeedStatus.NotEnough;
            }

            execute.UpdatedDate = TimeHelper.Now;
            executeRepo.Update(execute);
            try
            {
                _unitOfWork.Complete();
            } catch (Exception e)
            {
                throw new Exception($"{e}");
            }

            return GetById(executeId);
        }

        #endregion

        #region Start (tạo SubExecute cho Execute tổng)
        // Bắt đầu lệnh thực thi tổng cho 1 kế hoạch đề ra
        public ExecuteDto Start(StartExecuteRequest request, ClaimsPrincipal currentUser)
        {
            //kiểm tra trạng thái xác nhận của kế hoạch planning
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var planning = planningRepo.GetSingle(p => p.Id == request.PlanningId && !p.IsDeleted,
                includes: new[] { "CommandCuttingFile", "Machine" });
            if (planning == null) throw new Exception("Kế hoạch không tồn tại");
            if (planning.Status != PlanningStatus.Completed)
                throw new Exception("Kế hoạch phải ở trạng thái hoàn thành mới có thể thực thi");
            if (!planning.MachineId.HasValue)
                throw new Exception("Kế hoạch chưa được phân công máy");

            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var execute = executeRepo.GetSingle(e => e.PlanningId == planning.Id && !e.IsDeleted);
            if (execute == null)
                throw new Exception("Lệnh thực thi tổng chưa được tạo");
            if (execute.ReceiveStatus == ReceiveMaterialFeedStatus.NotReceive)
                throw new Exception("Chưa có phôi, không thể bắt đầu thực thi");

            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var machineBusy = subExecuteRepo.GetAll(
                se => !se.IsDeleted && se.Status == ExecuteStatus.UnCompleted,
                includes: new[] { "Execute.Planning" })
                .Any(se => se.Execute.Planning.MachineId == planning.MachineId.Value);
            if (machineBusy)
                throw new Exception("Máy đang có lệnh thực thi chưa hoàn thành, cần hoàn thành trước khi bắt đầu lệnh mới");

            var mfpRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var hasAvailablePhoi = mfpRepo.Exists(m =>
                m.PlanningId == planning.Id
                && m.IsDirector
                && (m.Status == MaterialFeeding.Preparation || m.Status == MaterialFeeding.UnCompleted)
                && !m.IsDeleted);
            if (!hasAvailablePhoi)
                throw new Exception("Không có phôi khả dụng để thực thi");

            var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);
            var subExecute = new SubExecute
            {
                Id = Guid.NewGuid(),
                ExecuteId = execute.Id,
                UserId = currentUserId,
                Status = ExecuteStatus.UnCompleted,
                StartTime = TimeHelper.Now,
                CreatedDate = TimeHelper.Now,
                Type = ExecuteType.Start
            };

            subExecuteRepo.Add(subExecute);

            execute.Status = ExecuteStatus.Active;
            if (execute.StartTime == default)
            {
                execute.StartTime = TimeHelper.Now;
            }
            execute.UpdatedDate = TimeHelper.Now;
            executeRepo.Update(execute);

            _unitOfWork.Complete();
            return GetById(execute.Id);
        }

        #endregion

        #region Complete (kết thúc SubExecute, cập nhật Execute tổng)

        public ExecuteDto Complete(Guid id, CompleteExecuteRequest request)
        {
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecute = subExecuteRepo.GetSingle(se => se.Id == id && !se.IsDeleted,
                includes: new[] { "Execute.Planning.CommandCuttingFile", "Execute.Planning.Machine" });
            if (subExecute == null) throw new Exception("Lệnh thực thi con không tồn tại");

            if (subExecute.Status == ExecuteStatus.Completed)
                throw new Exception("Lệnh thực thi con đã hoàn thành");

            var execute = subExecute.Execute;
            if (execute == null || execute.IsDeleted)
                throw new Exception("Lệnh thực thi tổng không tồn tại");

            var totalFed = _materialFeedingPlanService.GetTotalFedQuantity(execute.PlanningId);
            var totalMaterialUsed = subExecuteRepo.GetAll(
                se => se.ExecuteId == execute.Id
                && se.Id != id
                && se.Status == ExecuteStatus.Completed
                && !se.IsDeleted)
                .Sum(se => se.MaterialUsed);
            var remainingMaterial = totalFed - totalMaterialUsed;

            if (request.ActualQuantity <= 0)
                throw new Exception("Số lượng thực tế phải lớn hơn 0");

            subExecute.Status = ExecuteStatus.Completed;
            subExecute.EndTime = TimeHelper.Now;
            subExecute.ActualQuantity = request.ActualQuantity;
            subExecute.MaterialUsed = request.ActualQuantity;
            subExecute.Reason = request.Reason;
            subExecute.UpdatedDate = TimeHelper.Now;
            subExecuteRepo.Update(subExecute);

            var allSubExecutes = subExecuteRepo.GetAll(
                se => se.ExecuteId == execute.Id && !se.IsDeleted).ToList();

            var totalActual = allSubExecutes
                .Where(se => se.Status == ExecuteStatus.Completed)
                .Sum(se => se.ActualQuantity) + request.ActualQuantity;
            var totalActualTime = allSubExecutes
                .Where(se => se.Status == ExecuteStatus.Completed && se.EndTime.HasValue)
                .Sum(se => (decimal)(se.EndTime.Value - se.StartTime).TotalMinutes)
                + (decimal)(TimeHelper.Now - subExecute.StartTime).TotalMinutes;

            execute.ActualQuantity = totalActual;
            execute.ActualTime = totalActualTime;
            execute.EndTime = TimeHelper.Now;
            execute.UpdatedDate = TimeHelper.Now;

            var ccfQuantity = execute.Planning?.CommandCuttingFile?.Quantity ?? 0;
            if (totalActual >= ccfQuantity)
            {
                execute.Status = ExecuteStatus.Completed;
            }

            var executeRepo = _unitOfWork.GetRepository<Execute>();
            executeRepo.Update(execute);

            if (execute.Status == ExecuteStatus.Completed)
            {
                var planningRepo = _unitOfWork.GetRepository<Planning>();
                var planning = planningRepo.GetById(execute.PlanningId);
                if (planning != null)
                {
                    planning.Status = PlanningStatus.Done;
                    planning.UpdatedDate = TimeHelper.Now;
                    planningRepo.Update(planning);
                }

                var ccfId = execute.Planning?.CommandCuttingFileId;
                if (ccfId.HasValue)
                {
                    _commandCuttingFileService.UpdateCompletedQuantity(ccfId.Value, totalActual);
                }
            }

            _unitOfWork.Complete();
            return GetById(execute.Id);
        }

        #endregion

        #region StartSubExecute / EndSubExecute / ContinuteExecute
        // Chia nhỏ từng đợt thực thi
        public SubExecuteDto StartSubExecute(StartSubExecuteRequest request, ClaimsPrincipal currentUser)
        {
            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var execute = executeRepo.GetSingle(e => e.Id == request.ExecuteId && !e.IsDeleted,
                includes: new[] { "Planning.CommandCuttingFile", "Planning.Machine" });
            if (execute == null) throw new Exception("Lệnh thực thi tổng không tồn tại");

            if (execute.Status == ExecuteStatus.Completed)
                throw new Exception("Lệnh thực thi tổng đã hoàn thành");

            if (execute.ReceiveStatus == ReceiveMaterialFeedStatus.NotReceive)
                throw new Exception("Chưa có phôi, không thể bắt đầu thực thi");

            if (!execute.Planning.MachineId.HasValue)
                throw new Exception("Kế hoạch chưa được phân công máy");

            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var machineBusy = subExecuteRepo.GetAll(
                se => !se.IsDeleted && se.Status == ExecuteStatus.UnCompleted,
                includes: new[] { "Execute.Planning" })
                .Any(se => se.Execute.Planning.MachineId == execute.Planning.MachineId.Value);

            if (request.Type == ExecuteType.Start)
            {
                if (machineBusy)
                    throw new Exception("Máy đang có lệnh thực thi chưa kết thúc, cần hoàn thành trước khi bắt đầu lệnh mới");
            }
            else
            {
                if (!machineBusy)
                    throw new Exception("Máy chưa có lệnh tạm dừng, cần tạm dừng trước khi tiếp tục");
            }

            var mfpRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var hasAvailablePhoi = mfpRepo.Exists(m =>
                m.PlanningId == execute.PlanningId
                && m.IsDirector
                && (m.Status == MaterialFeeding.Completed || m.Status == MaterialFeeding.UnCompleted)
                && !m.IsDeleted);
            if (!hasAvailablePhoi)
                throw new Exception("Không có phôi khả dụng để thực thi");

            var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);
            var subExecute = new SubExecute
            {
                Id = Guid.NewGuid(),
                ExecuteId = execute.Id,
                UserId = currentUserId,
                Status = ExecuteStatus.UnCompleted,
                StartTime = TimeHelper.Now,
                CreatedDate = TimeHelper.Now,
                Reason = request.Reason,
                Type = request.Type
            };

            subExecuteRepo.Add(subExecute);

            execute.Status = ExecuteStatus.Active;
            if (execute.StartTime == default)
            {
                execute.StartTime = TimeHelper.Now;
            }
            execute.UpdatedDate = TimeHelper.Now;
            executeRepo.Update(execute);

            _unitOfWork.Complete();
            return MapSubExecuteToDto(subExecute);
        }


        public SubExecuteDto EndSubExecute(Guid executeId, EndSubExecuteRequest request, ClaimsPrincipal currentUser)
        {
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecutes = subExecuteRepo.GetAll(se => se.ExecuteId == executeId && !se.IsDeleted,
                includes: new[] { "Execute.Planning.CommandCuttingFile", "Execute.Planning.Machine" }).ToList();

            var subExecuteStarting = subExecutes.FirstOrDefault(se => se.Status == ExecuteStatus.UnCompleted && se.Type == ExecuteType.Start);
            if (subExecuteStarting == default)
                throw new Exception("Không có lệnh thực thi nào đang bắt đầu");

            var subExecuteResume = subExecutes.FirstOrDefault(se => se.Status == ExecuteStatus.UnCompleted && se.Type == ExecuteType.Resume);
            if (subExecuteResume != default)
            {
                subExecuteResume.Status = ExecuteStatus.Completed;
                subExecuteResume.EndTime = TimeHelper.Now;
                subExecuteResume.Reason = request.Reason;
                subExecuteResume.UpdatedDate = TimeHelper.Now;
                subExecuteRepo.Update(subExecuteResume);
            }

            var execute = subExecuteStarting.Execute;
            if (execute == null || execute.IsDeleted)
                throw new Exception("Lệnh thực thi tổng không tồn tại");

            //if (request.ActualQuantity <= 0)
            //    throw new Exception("Số lượng thực tế phải lớn hơn 0");

            if (request.MaterialUsed <= 0)
                throw new Exception("Số phôi đã dùng phải lớn hơn 0");

            var receiveMaterialFeedRepo = _unitOfWork.GetRepository<ReceiveMaterialFeed>();
            var totalFed = receiveMaterialFeedRepo.GetAll(r => r.ExecuteId == execute.Id && !r.IsDeleted).Sum(x => x.QuantityConfirmReceived);
            var totalMaterialUsed = subExecuteRepo.GetAll(
                se => se.ExecuteId == execute.Id
                && se.Id != subExecuteStarting.Id
                && se.Status == ExecuteStatus.Completed
                && !se.IsDeleted)
                .Sum(se => se.MaterialUsed);
            var remainingMaterial = totalFed - totalMaterialUsed;

            if (request.MaterialUsed > remainingMaterial)
                throw new Exception($"Số phôi đã dùng ({request.MaterialUsed}) vượt quá phôi còn lại ({remainingMaterial})");

            subExecuteStarting.Status = ExecuteStatus.Completed;
            subExecuteStarting.EndTime = TimeHelper.Now;
            subExecuteStarting.ActualQuantity = request.MaterialUsed;
            subExecuteStarting.MaterialUsed = request.MaterialUsed;
            subExecuteStarting.Reason = request.Reason;
            subExecuteStarting.UpdatedDate = TimeHelper.Now;
            //subExecuteRepo.Update(subExecuteStarting);

            var allSubExecutes = subExecuteRepo.GetAll(
                se => se.ExecuteId == execute.Id && !se.IsDeleted).ToList();

            var totalActual = allSubExecutes
                .Where(se => se.Status == ExecuteStatus.Completed)
                .Sum(se => se.ActualQuantity) + request.MaterialUsed;
            var totalActualTime = allSubExecutes
                .Where(se => se.Status == ExecuteStatus.Completed && se.EndTime.HasValue)
                .Sum(se => (decimal)(se.EndTime.Value - se.StartTime).TotalMinutes)
                + (decimal)(TimeHelper.Now - subExecuteStarting.StartTime).TotalMinutes;

            execute.ActualQuantity = totalActual;
            execute.ActualTime = totalActualTime;
            execute.EndTime = TimeHelper.Now;
            execute.UpdatedDate = TimeHelper.Now;

            var ccfQuantity = execute.Planning?.CommandCuttingFile?.Quantity ?? 0;
            if (totalActual >= ccfQuantity)
            {
                execute.Status = ExecuteStatus.Completed;
            }
            else if (totalActual > 0)
            {
                execute.Status = ExecuteStatus.CompletedAPart;
            }

            var executeRepo = _unitOfWork.GetRepository<Execute>();
            //executeRepo.Update(execute);

            if (execute.Status == ExecuteStatus.Completed)
            {
                var planningRepo = _unitOfWork.GetRepository<Planning>();
                var planning = execute.Planning;
                if (planning != null)
                {
                    planning.Status = PlanningStatus.Done;
                    planning.UpdatedDate = TimeHelper.Now;
                    //planningRepo.Update(planning);
                }

                var ccfId = execute.Planning?.CommandCuttingFileId;
                if (ccfId.HasValue)
                {
                    //_commandCuttingFileService.UpdateCompletedQuantity(ccfId.Value, totalActual);
                    var item = _unitOfWork.GetRepository<CommandCuttingFile>().GetSingle(c => c.Id == ccfId.Value && !c.IsDeleted);
                    if (item == null) throw new Exception("Lệnh file cắt không tồn tại");

                    item.CompletedQuantity += totalActual;
                    item.UpdatedDate = TimeHelper.Now;

                    if (item.CompletedQuantity >= item.Quantity)
                        item.Status = ProductionOrderStatus.Completed;
                }
            }
            subExecuteRepo.Update(subExecuteStarting);
            _unitOfWork.Complete();
            return MapSubExecuteToDto(subExecuteStarting);
        }
        public SubExecuteDto ContinuteSubExecute(Guid executeId,  ClaimsPrincipal currentUser)
        {
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecuteResume = subExecuteRepo.GetSingle(
                se => se.ExecuteId == executeId && se.Status == ExecuteStatus.UnCompleted && se.Type == ExecuteType.Resume && !se.IsDeleted,
                includes: new[] { "Execute.Planning" });

            if (subExecuteResume == null)
                throw new Exception("Không có lệnh tạm dừng nào tồn tại");

            subExecuteResume.Status = ExecuteStatus.Completed;
            subExecuteResume.EndTime = TimeHelper.Now;
            subExecuteResume.UpdatedDate = TimeHelper.Now;
            subExecuteRepo.Update(subExecuteResume);

            var execute = subExecuteResume.Execute;
            if (execute == null || execute.IsDeleted)
                throw new Exception("Lệnh thực thi tổng không tồn tại");

            var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);
            var newSubExecute = new SubExecute
            {
                Id = Guid.NewGuid(),
                ExecuteId = execute.Id,
                UserId = currentUserId,
                Status = ExecuteStatus.UnCompleted,
                StartTime = TimeHelper.Now,
                CreatedDate = TimeHelper.Now,
                Type = ExecuteType.Start
            };
            subExecuteRepo.Add(newSubExecute);

            execute.Status = ExecuteStatus.Active;
            execute.UpdatedDate = TimeHelper.Now;
            var executeRepo = _unitOfWork.GetRepository<Execute>();
            executeRepo.Update(execute);

            _unitOfWork.Complete();
            return MapSubExecuteToDto(newSubExecute);
        }
        public void Completed(Guid executeId)
        {
            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var execute = executeRepo.GetSingle(whereCondition: e => e.Id == executeId && !e.IsDeleted, includes: new[] {"Planning", "Planning.CommandCuttingFile"});
            if (execute == null) throw new Exception("Lệnh thực thi không tồn tại");
            var ccfPlannedQuantity = execute.Planning?.CommandCuttingFile?.PlannedQuantity ?? 0;
            var ccfQuantity = execute.Planning?.CommandCuttingFile?.Quantity ?? 0;
            if (ccfPlannedQuantity > execute.ActualQuantity)
            {
                throw new Exception($"Số lượng chi tiết thực tế ${(execute.ActualQuantity)} chưa đảm bảo theo kế hoạch ${(ccfPlannedQuantity)}");
            }
            
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecutes = subExecuteRepo.GetAll(se => se.ExecuteId == executeId && !se.IsDeleted).ToList();
            var materialUseds = subExecutes.Sum(se => se.MaterialUsed);
            if (materialUseds <= ccfQuantity)
            {
                throw new Exception($"Số lượng phôi dùng thực tế ${(materialUseds)} đang ít hơn số lượng phôi dự kiến ${(ccfQuantity)}");
            }
            execute.Status = ExecuteStatus.Completed;
            execute.UpdatedDate = TimeHelper.Now;
            executeRepo.Update(execute);
            _unitOfWork.Complete();
        }

        #endregion

        #region Cancel

        public ExecuteDto Cancel(Guid id, CancelExecuteRequest request)
        {
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecute = subExecuteRepo.GetSingle(se => se.Id == id && !se.IsDeleted,
                includes: new[] { "Execute.Planning.CommandCuttingFile", "Execute.Planning.Machine" });

            Execute execute;
            if (subExecute != null)
            {
                execute = subExecute.Execute;
                if (subExecute.Status == ExecuteStatus.Completed)
                    throw new Exception("Không thể hủy lệnh thực thi con đã hoàn thành");

                subExecute.Status = ExecuteStatus.Completed;
                subExecute.EndTime = TimeHelper.Now;
                subExecute.Reason = request.Reason;
                subExecute.UpdatedDate = TimeHelper.Now;
                subExecuteRepo.Update(subExecute);
            }
            else
            {
                var executeRepo = _unitOfWork.GetRepository<Execute>();
                execute = executeRepo.GetSingle(e => e.Id == id && !e.IsDeleted,
                    includes: new[] { IncludePlanning, IncludePlanningCommand, IncludePlanningMachine, IncludeUser });
                if (execute == null) throw new Exception("Lệnh thực thi không tồn tại");

                if (execute.Status == ExecuteStatus.Completed)
                    throw new Exception("Không thể hủy lệnh thực thi đã hoàn thành");
            }

            var activeSubExecutes = subExecuteRepo.GetAll(
                se => se.ExecuteId == execute.Id && se.Status == ExecuteStatus.UnCompleted && !se.IsDeleted).ToList();

            if (!activeSubExecutes.Any())
            {
                execute.Status = ExecuteStatus.Completed;
                execute.IsOverdue = true;
                execute.EndTime = TimeHelper.Now;
                execute.Reason = request.Reason;
                execute.UpdatedDate = TimeHelper.Now;

                var executeRepo2 = _unitOfWork.GetRepository<Execute>();
                executeRepo2.Update(execute);

                var planningRepo = _unitOfWork.GetRepository<Planning>();
                var planning = planningRepo.GetById(execute.PlanningId);
                if (planning != null)
                {
                    planning.Status = PlanningStatus.Cancelled;
                    planning.UpdatedDate = TimeHelper.Now;
                    planningRepo.Update(planning);
                }
            }

            _unitOfWork.Complete();
            return GetById(execute.Id);
        }

        #endregion

        #region Summary

        public SummaryStatusExecute Summary()
        {
            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var allExecute = executeRepo.GetAll(e => !e.IsDeleted).ToList();
            return new SummaryStatusExecute
            {
                TotalCompleted = allExecute.Count(x => x.Status == ExecuteStatus.Completed),
                TotalUnCompleted = allExecute.Count(x => x.Status == ExecuteStatus.UnCompleted),
                TotalActive = allExecute.Count(x => x.Status == ExecuteStatus.Active),
                TotalCompletedAPart = allExecute.Count(x => x.Status == ExecuteStatus.CompletedAPart)
            };
        }

        #endregion

        #region Mapping

        private ExecuteDto MapToDto(Execute e)
        {
            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var subExecutes = subExecuteRepo.GetAll(
                se => se.ExecuteId == e.Id && !se.IsDeleted,
                includes: new[] { "User" })
                .OrderBy(se => se.StartTime)
                .ToList();
            var receiveMaterialRepo = _unitOfWork.GetRepository<ReceiveMaterialFeed>();
            var allReceiveMaterial = receiveMaterialRepo.GetAll(r => r.ExecuteId == e.Id);
            var quantityReceive =  allReceiveMaterial != null && allReceiveMaterial.Any() ?allReceiveMaterial.Sum(r => r.QuantityConfirmReceived) : 0;
            var materialFeedingPlanRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var materialFeedingPlans = materialFeedingPlanRepo.GetAll(m => m.PlanningId == e.PlanningId && m.IsDirector).Sum(x => x.QuantityActual);
            return new ExecuteDto
            {
                Id = e.Id,
                PlanningId = e.PlanningId,
                StartTime = e.Planning.StartTime,
                EndTime = e.Planning.EndTime,
                CuttingFileCode = e.Planning?.CommandCuttingFile?.CuttingFile?.Code,
                CuttingFileName = e.Planning?.CommandCuttingFile?.CuttingFile?.Name,
                // [THAY ĐỔI] Lấy ProductionCommandCode từ collection (FirstOrDefault vì 1 CCF có nhiều PC)
                ProductionCommandCode = e.Planning?.CommandCuttingFile?.ProductionCommands?.FirstOrDefault()?.CommandCode,
                MachineCode = e.Planning?.Machine?.MachineCode,
                MachineName = e.Planning?.Machine?.MachineName,
                UserId = e.UserId,
                UserName = e.User?.UserName,
                FullName = e.User?.FullName,
                Status = (int)e.Status,
                StatusName = e.Status.ToString(),
                ReceiveStatus = (int)e.ReceiveStatus,
                ReceiveStatusName = e.ReceiveStatus.ToString(),
                ExecuteStartTime = e.StartTime,
                ExecuteEndTime = e.EndTime,
                IsOverdue = e.IsOverdue,
                Reason = e.Reason,
                ActualQuantity = e.ActualQuantity,
                ActualTime = e.ActualTime,
                PlannedQuantity = e.Planning?.CommandCuttingFile?.Quantity ?? 0,
                MaterialCode = e.Planning?.CommandCuttingFile?.MaterialCode,
                Length = e.Planning?.CommandCuttingFile?.Length,
                Width = e.Planning?.CommandCuttingFile?.Width,
                Thickness = e.Planning?.CommandCuttingFile?.Thickness,
                Quantity = e.Planning.CommandCuttingFile.Quantity,
                QuantityReceive = quantityReceive,
                QuantityNotConfirm = materialFeedingPlans,
                SubExecutes = subExecutes.Select(MapSubExecuteToDto).ToList()
            };
        }

        private SubExecuteDto MapSubExecuteToDto(SubExecute se)
        {
            return new SubExecuteDto
            {
                Id = se.Id,
                ExecuteId = se.ExecuteId,
                UserId = se.UserId,
                UserName = se.User?.UserName,
                FullName = se.User?.FullName,
                Status = (int)se.Status,
                StatusName = se.Status.ToString(),
                StartTime = se.StartTime,
                EndTime = se.EndTime,
                ActualQuantity = se.ActualQuantity,
                MaterialUsed = se.MaterialUsed,
                Reason = se.Reason,
                Type = se.Type
            };
        }

        #endregion
    }
}