using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CommandCuttingFile;
using CoKhiThaCo.Models.Dtos.SummaryPlan;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Services
{
    public interface ISummaryPlanService
    {
        PagedResult<SummaryPlanDto> GetSummary(SummaryRequest query);
    }

    public class SummaryPlanService : ISummaryPlanService
    {
        private readonly IUnitOfWork _unitOfWork;

        private static readonly TimeSpan WorkingDayStartTime = new TimeSpan(7, 30, 0);

        public SummaryPlanService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // [THAY ĐỔI] Include: "ProductionCommands" (collection) thay vì "ProductionCommand" (nav đơn)
        private readonly string[] _planningIncludes = new[]
        {
            "CommandCuttingFile.CuttingFile.CategoryCuttingFile",
            "CommandCuttingFile.ProductionCommands",
            "Machine",
            "Segments"
        };

        public PagedResult<SummaryPlanDto> GetSummary(SummaryRequest query)
        {
            var planningRepo = _unitOfWork.GetRepository<Planning>();
            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new SortField { Field = "StartTime", Direction = "desc" });

            var planningQuery = planningRepo.GetAll(
                p => !p.IsDeleted,
                includes: _planningIncludes);

            if (query.MachineId.HasValue)
            {
                var machineId = query.MachineId.Value;
                planningQuery = planningQuery.Where(p => p.MachineId == machineId).ToList();
            }

            if (query.Status.HasValue)
            {
                var status = (PlanningStatus)query.Status.Value;
                planningQuery = planningQuery.Where(p => p.Status == status).ToList();
            }

            if (query.DateFrom.HasValue)
            {
                var dateFrom = query.DateFrom.Value;
                planningQuery = planningQuery.Where(p => p.StartTime >= dateFrom).ToList();
            }

            if (query.DateTo.HasValue)
            {
                var dateTo = query.DateTo.Value.AddDays(1);
                planningQuery = planningQuery.Where(p => p.StartTime < dateTo).ToList();
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.ToLower();
                planningQuery = planningQuery.Where(p =>
                    (p.CommandCuttingFile?.CuttingFile?.Name != null && p.CommandCuttingFile.CuttingFile.Name.ToLower().Contains(keyword)) ||
                    (p.CommandCuttingFile?.CuttingFile?.Code != null && p.CommandCuttingFile.CuttingFile.Code.ToLower().Contains(keyword)) ||
                    // [THAY ĐỔI] Query keyword: dùng ProductionCommands.Any() thay vì ProductionCommand.CommandCode
                    (p.CommandCuttingFile?.ProductionCommands != null && p.CommandCuttingFile.ProductionCommands.Any(pc => pc.CommandCode.ToLower().Contains(keyword))) ||
                    (p.Machine?.MachineCode != null && p.Machine.MachineCode.ToLower().Contains(keyword)) ||
                    (p.Machine?.MachineName != null && p.Machine.MachineName.ToLower().Contains(keyword))
                ).ToList();
            }

            var plannings = planningQuery.ToList();

            var executeRepo = _unitOfWork.GetRepository<Execute>();
            var planningIds = plannings.Select(p => p.Id).ToList();
            var executes = executeRepo.GetAll(
                e => planningIds.Contains(e.PlanningId) && !e.IsDeleted,
                includes: new[] { "Planning", "Planning.CommandCuttingFile", "Planning.CommandCuttingFile.ProductionCommands" })
                .ToList();
            var executeByPlanning = executes.GroupBy(e => e.PlanningId).ToDictionary(g => g.Key, g => g.First());

            var subExecuteRepo = _unitOfWork.GetRepository<SubExecute>();
            var executeIds = executes.Select(e => e.Id).ToList();
            var subExecutes = subExecuteRepo.GetAll(
                se => executeIds.Contains(se.ExecuteId) && !se.IsDeleted)
                .ToList();
            var subExecuteGroup = subExecutes.GroupBy(se => se.ExecuteId).ToDictionary(g => g.Key, g => g.ToList());

            var receiveRepo = _unitOfWork.GetRepository<ReceiveMaterialFeed>();
            var receives = receiveRepo.GetAll(
                r => executeIds.Contains(r.ExecuteId) && !r.IsDeleted)
                .ToList();
            var receiveGroup = receives.GroupBy(r => r.ExecuteId).ToDictionary(g => g.Key, g => g.Sum(r => r.QuantityConfirmReceived));

            var mfpRepo = _unitOfWork.GetRepository<MaterialFeedingPlan>();
            var mfps = mfpRepo.GetAll(
                m => planningIds.Contains(m.PlanningId) && m.IsDirector && !m.IsDeleted)
                .ToList();
            var mfpGroup = mfps.GroupBy(m => m.PlanningId).ToDictionary(g => g.Key, g => g.Sum(m => m.QuantityActual));

            var ccfRepo = _unitOfWork.GetRepository<CommandCuttingFile>();
            var ccfIds = plannings.Select(p => p.CommandCuttingFileId).Distinct().ToList();
            var ccfs = ccfRepo.GetAll(c => ccfIds.Contains(c.Id) && !c.IsDeleted).ToDictionary(c => c.Id);

            // [THAY ĐỔI] Lấy productionCommandIds từ collection ProductionCommands thay vì FK đơn
            //var productionCommandRepo = _unitOfWork.GetRepository<ProductionCommand>();
            //var productionCommandIds = ccfs.Values
            //    .SelectMany(c => c.ProductionCommands ?? new List<ProductionCommand>())
            //    .Select(pc => pc.Id)
            //    .Distinct()
            //    .ToList();
            //var productionCommands = productionCommandRepo.GetAll(pc => productionCommandIds.Contains(pc.Id) && !pc.IsDeleted).ToDictionary(pc => pc.Id);

            var result = new List<SummaryPlanDto>();
            foreach (var planning in plannings)
            {
                var ccf = ccfs.GetValueOrDefault(planning.CommandCuttingFileId);
                var execute = executeByPlanning.GetValueOrDefault(planning.Id);
                var totalFed = mfpGroup.GetValueOrDefault(planning.Id);
                var totalReceived = execute != null ? receiveGroup.GetValueOrDefault(execute.Id) : 0;

                var totalSubExecute = 0;
                var totalActualQuantity = 0;
                var totalMaterialUsed = 0;
                if (execute != null && subExecuteGroup.ContainsKey(execute.Id))
                {
                    var subs = subExecuteGroup[execute.Id];
                    totalSubExecute = subs.Count;
                    totalActualQuantity = subs.Where(s => s.Status == ExecuteStatus.Completed).Sum(s => s.ActualQuantity);
                    totalMaterialUsed = subs.Where(s => s.Status == ExecuteStatus.Completed).Sum(s => s.MaterialUsed);
                }

                var ccfQuantity = ccf?.Quantity ?? 0;
                var progressPercent = ccfQuantity > 0 ? Math.Round((decimal)totalActualQuantity / ccfQuantity * 100, 2) : 0;

                // [THAY ĐỔI] Map ProductionCommands từ collection thay vì FK đơn
                var productionCommandDtos = new List<CommandProductionCommandDto>();
                if (ccf?.ProductionCommands != null && ccf.ProductionCommands.Any())
                {
                    foreach (var pc in ccf.ProductionCommands)
                    {
                        productionCommandDtos.Add(new CommandProductionCommandDto
                        {
                            ProductionCommandId = pc.Id,
                            ProductionCommandCode = pc.CommandCode,
                            OrderName = pc.OrderName
                        });
                    }
                }

                var dto = new SummaryPlanDto
                {
                    PlanningId = planning.Id,
                    CommandCuttingFileId = planning.CommandCuttingFileId,
                    CuttingFileId = ccf?.CuttingFileId ?? Guid.Empty,
                    CuttingFileName = planning.CommandCuttingFile?.CuttingFile?.Name,
                    ProductionCommands = productionCommandDtos,
                    CategoryCuttingFileId = ccf?.CategoryCuttingFileId ?? Guid.Empty,
                    CategoryCuttingFileName = planning.CommandCuttingFile?.CuttingFile?.CategoryCuttingFile?.Name,
                    MaterialCode = ccf?.MaterialCode,
                    CuttingTime = ccf?.CuttingTime ?? 0,
                    Quantity = ccfQuantity,
                    ReceiveQuantity = totalReceived,
                    QuantityNotConfirm = Math.Max(0, totalFed - totalReceived),
                    StartTime = planning.StartTime,
                    EndTime = planning.EndTime,
                    ActualStartTime = execute?.StartTime ?? default,
                    ActualEndTime = execute?.EndTime,
                    MachineId = planning.MachineId ?? Guid.Empty,
                    MachineCode = planning.Machine?.MachineCode,
                    MachineName = planning.Machine?.MachineName,
                    statusCode = planning.Status,
                    StatusName = planning.Status.ToString(),
                    ExecuteId = execute?.Id,
                    ExecuteStatus = execute?.Status,
                    ExecuteStatusName = execute?.Status.ToString(),
                    ReceiveStatus = execute?.ReceiveStatus,
                    ReceiveStatusName = execute?.ReceiveStatus.ToString(),
                    TotalSubExecute = totalSubExecute,
                    TotalActualQuantity = totalActualQuantity,
                    TotalMaterialUsed = totalMaterialUsed,
                    IsOverdue = execute?.IsOverdue ?? false,
                    ProgressPercent = progressPercent
                };
                result.Add(dto);
            }

            var total = result.Count;

            if (sorts.Any())
            {
                var sort = sorts.First();
                var isDesc = sort.Direction?.ToLower() == "desc";

                result = sort.Field switch
                {
                    "StartTime" => isDesc ? result.OrderByDescending(r => r.StartTime).ToList() : result.OrderBy(r => r.StartTime).ToList(),
                    "EndTime" => isDesc ? result.OrderByDescending(r => r.EndTime).ToList() : result.OrderBy(r => r.EndTime).ToList(),
                    "StatusCode" => isDesc ? result.OrderByDescending(r => r.statusCode).ToList() : result.OrderBy(r => r.statusCode).ToList(),
                    "MachineCode" => isDesc ? result.OrderByDescending(r => r.MachineCode).ToList() : result.OrderBy(r => r.MachineCode).ToList(),
                    "ProgressPercent" => isDesc ? result.OrderByDescending(r => r.ProgressPercent).ToList() : result.OrderBy(r => r.ProgressPercent).ToList(),
                    _ => isDesc ? result.OrderByDescending(r => r.StartTime).ToList() : result.OrderBy(r => r.StartTime).ToList()
                };
            }

            var paged = result
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<SummaryPlanDto>
            {
                Items = paged,
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}