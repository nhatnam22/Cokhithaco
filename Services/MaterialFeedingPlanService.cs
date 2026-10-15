using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.MaterialFeedingPlan;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Services
{
    public interface IMaterialFeedingPlanService
    {
        PagedResult<MaterialFeedingPlanDto> GetPaged(MaterialFeedingPlanListQuery query);
        MaterialFeedingPlanDto GetById(Guid id);
        MaterialFeedingPlanDto Create(CreateMaterialFeedingPlanRequest request);
        MaterialFeedingPlanDto Update(Guid id, UpdateMaterialFeedingPlanRequest request);
        MaterialFeedingPlanDto Complete(Guid id, CompleteMaterialFeedingPlanRequest request, ClaimsPrincipal currentUser);
        bool Delete(Guid id);
        int GetTotalFedQuantity(Guid planningId);
        SummaryMaterialFeedingStatus Summary();
        SummaryTodayMaterialFeedingStatus SummaryToday();
    }

    public class MaterialFeedingPlanService : IMaterialFeedingPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionHelper _permissionHelper;
        private readonly string[] _materialFeedIncludes = new[]
        {
            "Planning",
            "Planning.Machine",
            "Planning.CommandCuttingFile",
            // [THAY ĐỔI] Include: "ProductionCommands" (collection) thay vì "ProductionCommand" (nav đơn)
            "Planning.CommandCuttingFile.ProductionCommands",
            "Planning.CommandCuttingFile.CuttingFile"
        };

        public MaterialFeedingPlanService(IUnitOfWork unitOfWork, IPermissionHelper permissionHelper)
        {
            _unitOfWork = unitOfWork;
            _permissionHelper = permissionHelper;
        }

        #region Helper Methods

        private MaterialFeedingPlan GetEntity(Guid id, string[] includes = null)
        {
            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            return repo.GetSingle(mf => mf.Id == id && !mf.IsDeleted, includes: includes);
        }

        private Planning ValidatePlanning(Guid planningId)
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var planning = planningRepo.GetSingle(p => p.Id == planningId && !p.IsDeleted,
                includes: new[] { "CommandCuttingFile" });
            if (planning == null)
                throw new Exception("Kế hoạch không tồn tại");
            if (planning.Status == PlanningStatus.Cancelled)
                throw new Exception("Không thể cấp phôi cho kế hoạch đã hủy");
            return planning;
        }

        private int GetTotalPlannedQuantity(Guid planningId)
        {
            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            return repo.GetAll(mf => mf.PlanningId == planningId && mf.Status != MaterialFeeding.Cancelled && !mf.IsDeleted)
                .Sum(mf => mf.Quantity);
        }

        #endregion

        #region CRUD

        public PagedResult<MaterialFeedingPlanDto> GetPaged(MaterialFeedingPlanListQuery query)
        {
            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            var isHasKeyword = string.IsNullOrEmpty(query.Keyword);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "CreatedDate", Direction = "desc" });

            Expression<Func<MaterialFeedingPlan, bool>> where = mf => !mf.IsDeleted && mf.IsDirector == true 
                // [THAY ĐỔI] Query keyword: dùng ProductionCommands.Any() thay vì ProductionCommand.CommandCode
                && (isHasKeyword || mf.Planning.CommandCuttingFile.ProductionCommands.Any(pc => pc.CommandCode.Contains(query.Keyword))
            || mf.Planning.CommandCuttingFile.MaterialCode.Contains(query.Keyword)); 
            if (query.PlanningId.HasValue)
            {
                var pId = query.PlanningId.Value;
                where = mf => !mf.IsDeleted && mf.PlanningId == pId;
            }
            if (query.Status.HasValue)
            {
                var status = (MaterialFeeding)query.Status.Value;
                if (query.PlanningId.HasValue)
                {
                    var pId = query.PlanningId.Value;
                    where = mf => !mf.IsDeleted && mf.PlanningId == pId && mf.Status == status;
                }
                else
                {
                    where = mf => !mf.IsDeleted && mf.Status == status;
                }
            }

            var items = repo.GetAllPaging(where, out var total, query.Page, query.PageSize, sorts, filters, _materialFeedIncludes);
            return new PagedResult<MaterialFeedingPlanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public MaterialFeedingPlanDto GetById(Guid id)
        {
            var item = GetEntity(id);
            return item == null ? null : MapToDto(item);
        }

        public MaterialFeedingPlanDto Create(CreateMaterialFeedingPlanRequest request)
        {
            if (request.Quantity <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            var planning = ValidatePlanning(request.PlanningId);

            var ccfQuantity = planning.CommandCuttingFile?.Quantity ?? 0;
            var totalPlanned = GetTotalPlannedQuantity(request.PlanningId);
            if (totalPlanned + request.Quantity > ccfQuantity)
                throw new Exception($"Tổng số lượng dự kiến cấp ({totalPlanned + request.Quantity}) vượt quá số lượng cần ({ccfQuantity})");

            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var item = new MaterialFeedingPlan
            {
                Id = Guid.NewGuid(),
                PlanningId = request.PlanningId,
                Quantity = request.Quantity,
                QuantityActual = 0,
                Status = MaterialFeeding.Preparation,
                IsDirector = request.IsDirector,
                CreatedDate = TimeHelper.Now,
                //Length = request.Length.Value,
                //Thickness = request.Thickness.Value,
                //Width = request.Width.Value
            };

            repo.Add(item);
            _unitOfWork.Complete();
            return GetById(item.Id);
        }

        public MaterialFeedingPlanDto Update(Guid id, UpdateMaterialFeedingPlanRequest request)
        {
            var item = GetEntity(id);
            if (item == null) return null;

            if (item.Status != MaterialFeeding.UnCompleted)
                throw new Exception("Chỉ có thể cập nhật khi chưa hoàn thành");

            if (request.Quantity <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            var planning = ValidatePlanning(item.PlanningId);
            var ccfQuantity = planning.CommandCuttingFile?.Quantity ?? 0;
            var otherPlanned = GetTotalPlannedQuantity(item.PlanningId) - item.Quantity;
            if (otherPlanned + request.Quantity > ccfQuantity)
                throw new Exception($"Tổng số lượng dự kiến cấp ({otherPlanned + request.Quantity}) vượt quá số lượng cần ({ccfQuantity})");

            if (request.QuantityActual > item.Quantity) throw new Exception($"Tổng lượng cấp thực tế ({request.QuantityActual}) không được lớn hơn lượng cần ({ccfQuantity}) ");
            item.Status = request.QuantityActual < item.Quantity ? MaterialFeeding.UnCompleted : request.QuantityActual == item.Quantity ? MaterialFeeding.Completed : MaterialFeeding.Completed;
            item.Quantity = request.Quantity;
            item.UpdatedDate = TimeHelper.Now;
            //item.Length = request.Length.Value;
            //item.Thickness = request.Thickness.Value;
            //item.Width = request.Width.Value;
            //item.Description = request.Description;

            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            repo.Update(item);
            _unitOfWork.Complete();
            return GetById(id);
        }

        public MaterialFeedingPlanDto Complete(Guid id, CompleteMaterialFeedingPlanRequest request, ClaimsPrincipal currentUser)
        {
            var item = GetEntity(id);
            if (item == null) throw new Exception("Kế hoạch cấp phôi không tồn tại");

            if (item.Status != MaterialFeeding.Preparation && item.Status != MaterialFeeding.UnCompleted)
                throw new Exception("Chỉ có thể xác nhận khi đang chuẩn bị và chưa cấp đủ");
            if (request.Length.HasValue)
            {
                if (string.IsNullOrEmpty(request.Description)) throw new Exception("Hãy nhập mô tả hoặc lý do cập nhật");
            }
            if (request.QuantityActual <= 0)
                throw new Exception("Số lượng thực tế phải lớn hơn 0");

            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var subItem = repo.GetSingle(m => m.PlanningId == item.PlanningId && m.Id != id && !m.IsDirector && m.Status == MaterialFeeding.Preparation && m.QuantityActual == 0);

            var totalAfter = item.QuantityActual + request.QuantityActual;
            if (totalAfter > item.Quantity)
                throw new Exception($"Tổng số lượng thực tế ({totalAfter}) vượt quá số lượng dự kiến ({item.Quantity})");

            var currentUserId = _permissionHelper.GetCurrentUserId(currentUser);

            item.QuantityActual = totalAfter;
            item.Status = item.QuantityActual >= item.Quantity ? MaterialFeeding.Completed : MaterialFeeding.UnCompleted;
            item.TimeAtCompleted = TimeHelper.Now;
            item.UserId = currentUserId;
            item.UpdatedDate = TimeHelper.Now;
            item.Length = request.Length.Value;
            item.Width = request.Width.Value;
            item.Thickness = request.Thickness.Value;
            item.Description = request.Description;
            repo.Update(item);

            //if (subItem != null)
            //{
            //    repo.Delete(subItem.Id);
            //}

            if (item.QuantityActual < item.Quantity)
            {
                var remaining = new MaterialFeedingPlan
                {
                    Id = Guid.NewGuid(),
                    PlanningId = item.PlanningId,
                    Quantity = item.Quantity - item.QuantityActual,
                    QuantityActual = 0,
                    Status = MaterialFeeding.Preparation,
                    IsDirector = false,
                    CreatedDate = TimeHelper.Now
                };
                repo.Add(remaining);
            }

            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var item = GetEntity(id);
            if (item == null) return false;

            if (item.Status != MaterialFeeding.Preparation)
                throw new Exception("Chỉ có thể xóa khi đang chuẩn bị");

            item.IsDeleted = true;
            item.DeletedDate = TimeHelper.Now;

            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            repo.Update(item);
            _unitOfWork.Complete();
            return true;
        }

        public int GetTotalFedQuantity(Guid planningId)
        {
            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            return repo.GetAll(mf => mf.PlanningId == planningId && mf.Status == MaterialFeeding.Completed && !mf.IsDeleted && mf.IsDirector == true)
                .Sum(mf => mf.QuantityActual);
        }
        public SummaryMaterialFeedingStatus Summary()
        {
            var materialFeedingPlanRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var allMaterialFeedingPlan = materialFeedingPlanRepo.GetAll(m => !m.IsDeleted).ToList();
            return new SummaryMaterialFeedingStatus
            {
                TotalPrepagation = allMaterialFeedingPlan.Count(x => x.Status == MaterialFeeding.Preparation),
                TotalCompleted = allMaterialFeedingPlan.Count(x => x.Status == MaterialFeeding.Completed),
                TotalCancel = allMaterialFeedingPlan.Count(x => x.Status == MaterialFeeding.Cancelled)
            };
        }

        public SummaryTodayMaterialFeedingStatus SummaryToday()
        {
            var workingDayStart = TimeHelper.Today + new TimeSpan(7, 30, 0);
            var workingDayEnd = workingDayStart.AddDays(1);
            var now = TimeHelper.Now;

            var repo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var todayPlans = repo.GetAll(
                mf => !mf.IsDeleted
                    && mf.Status != MaterialFeeding.Cancelled
                    && mf.Planning.StartTime >= workingDayStart
                    && mf.Planning.StartTime < workingDayEnd,
                includes: new[] { "Planning" })
                .ToList();

            var needFeeding = todayPlans.Where(mf => mf.Status == MaterialFeeding.Preparation).ToList();
            var fedEnough = todayPlans.Count(mf => mf.Status == MaterialFeeding.Completed && mf.QuantityActual >= mf.Quantity);

            return new SummaryTodayMaterialFeedingStatus
            {
                TotalNeedFeeding = needFeeding.Count,
                TotalFedEnough = fedEnough,
                TotalNotFedEnough = needFeeding.Count - fedEnough,
                TotalOverdue = needFeeding.Count(mf => mf.Planning.StartTime.AddHours(1) < now),
                TotalNotOverdue = needFeeding.Count(mf => mf.Planning.StartTime.AddHours(1) > now)
            };
        }

        #endregion

        #region Mapping

        private MaterialFeedingPlanDto MapToDto(MaterialFeedingPlan mf)
        {
            return new MaterialFeedingPlanDto
            {
                Id = mf.Id,
                PlanningId = mf.PlanningId,
                CommandCuttingFileCode = mf.Planning?.CommandCuttingFile?.CuttingFile?.Code,
                CuttingFileName = mf.Planning?.CommandCuttingFile?.CuttingFile?.Name,
                // [THAY ĐỔI] Lấy ProductionCommandCode từ collection (FirstOrDefault vì 1 CCF có nhiều PC)
                ProductionCommandCode = mf.Planning?.CommandCuttingFile?.ProductionCommands?.FirstOrDefault()?.CommandCode,
                MachineCode = mf.Planning?.Machine?.MachineCode,
                MachineName = mf.Planning?.Machine?.MachineName,
                UserId = mf.UserId,
                UserName = mf.User?.UserName,
                FullName = mf.User?.FullName,
                Status = (int)mf.Status,
                StatusName = mf.Status.ToString(),
                Quantity = mf.Quantity,
                QuantityActual = mf.QuantityActual,
                TimeAtCompleted = mf.TimeAtCompleted,
                CreatedDate = mf.CreatedDate,
                Length = mf.Planning?.CommandCuttingFile?.Length,
                Thickness = mf.Planning?.CommandCuttingFile?.Thickness,
                Width = mf.Planning?.CommandCuttingFile?.Width,
                MaterialCode = mf.Planning?.CommandCuttingFile?.MaterialCode,
                StartTime = mf.Planning?.StartTime
            };
        }
        #endregion
    }
}