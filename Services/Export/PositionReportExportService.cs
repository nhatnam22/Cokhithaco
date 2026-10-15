using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos.Export;
using CoKhiThaCo.Services.Export.Core;

namespace CoKhiThaCo.Services.Export
{
    public interface IPositionReportExportService : IExportService<PositionExportRequest, PositionExportDto>
    {
    }

    public class PositionReportExportService : BaseExportService<PositionExportRequest, PositionExportDto>, IPositionReportExportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PositionReportExportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public override IEnumerable<PositionExportDto> FetchData(PositionExportRequest request)
        {
            var repo = _unitOfWork.GetRepository<Position>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();

            var positions = repo.GetAll(p => !p.IsDeleted, orderBy: q => q.OrderBy(p => p.PositionName));
            int rowNo = 1;
            foreach (var p in positions)
            {
                var permissionCount = ppRepo.Count(pp => pp.PositionId == p.PositionId && !pp.IsDeleted);
                yield return new PositionExportDto
                {
                    RowNo = rowNo++,
                    PositionName = p.PositionName,
                    Description = p.Description,
                    UserCount = userRepo.Count(u => u.PositionId == p.PositionId && !u.IsDeleted),
                    PermissionCount = permissionCount,
                    CreatedDate = p.CreatedDate
                };
            }
        }

        protected override IExportBuilder<PositionExportDto> GetExcelBuilder(PositionExportRequest request)
        {
            return new PositionExcelBuilder();
        }

        protected override IExportBuilder<PositionExportDto> GetPdfBuilder(PositionExportRequest request)
        {
            return new PositionPdfBuilder();
        }
    }
}
