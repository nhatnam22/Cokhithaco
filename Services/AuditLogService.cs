using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Menu;

namespace CoKhiThaCo.Services
{
    public interface IAuditLogService
    {
        PagedResult<AuditLogDto> GetPaged(int page, int pageSize, string entityName = null, string action = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PagedResult<AuditLogDto> GetPaged(int page, int pageSize, string entityName = null, string action = null)
        {
            var repo = _unitOfWork.GetRepository<AuditLog>();
            var query = repo.GetAll(a =>
                (string.IsNullOrWhiteSpace(entityName) || a.EntityName == entityName) &&
                (string.IsNullOrWhiteSpace(action) || a.Action == action));

            var total = query.Count;
            var items = query
                .OrderByDescending(a => a.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Action = a.Action,
                    PropertyName = a.PropertyName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    ChangedBy = a.ChangedBy,
                    ChangedAt = a.ChangedAt
                })
                .ToList();

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
