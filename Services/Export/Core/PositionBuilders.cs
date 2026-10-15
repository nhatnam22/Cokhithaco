using System.Collections.Generic;
using CoKhiThaCo.Models.Dtos.Export;
using CoKhiThaCo.Services.Export.Core;

namespace CoKhiThaCo.Services.Export.Core
{
    public class PositionExcelBuilder : BaseExcelBuilder<PositionExportDto>
    {
        public PositionExcelBuilder()
        {
            DocumentTitle = "Danh sach chuc vu";
        }

        public override void DefineHeaders()
        {
            _columns.Clear();
            _columns.Add(new ColumnDefinition { HeaderText = "STT", Width = 8 });
            _columns.Add(new ColumnDefinition { HeaderText = "Ten chuc vu", Width = 40 });
            _columns.Add(new ColumnDefinition { HeaderText = "Mo ta", Width = 40 });
            _columns.Add(new ColumnDefinition { HeaderText = "So nguoi dung", Width = 15 });
            _columns.Add(new ColumnDefinition { HeaderText = "So quyen", Width = 12 });
            _columns.Add(new ColumnDefinition { HeaderText = "Ngay tao", Width = 18, NumberFormat = "dd/MM/yyyy" });
        }

        public override void WriteRow(PositionExportDto item, int rowIndex)
        {
            var row = _dataStartRow + rowIndex;
            SetCellValue(row, 1, item.RowNo);
            SetCellValue(row, 2, item.PositionName);
            SetCellValue(row, 3, item.Description);
            SetCellValue(row, 4, item.UserCount);
            SetCellValue(row, 5, item.PermissionCount);
            SetCellValue(row, 6, item.CreatedDate);
        }
    }

    public class PositionPdfBuilder : BasePdfBuilder<PositionExportDto>
    {
        public PositionPdfBuilder()
        {
            DocumentTitle = "Danh sach chuc vu";
        }

        public override void DefineHeaders()
        {
            _columns.Clear();
            _columns.Add(new ColumnDefinition { HeaderText = "STT", PdfAlign = "center" });
            _columns.Add(new ColumnDefinition { HeaderText = "Ten chuc vu", PdfAlign = "left" });
            _columns.Add(new ColumnDefinition { HeaderText = "Mo ta", PdfAlign = "left" });
            _columns.Add(new ColumnDefinition { HeaderText = "So NLD", PdfAlign = "center" });
            _columns.Add(new ColumnDefinition { HeaderText = "So quyen", PdfAlign = "center" });
            _columns.Add(new ColumnDefinition { HeaderText = "Ngay tao", PdfAlign = "center" });
        }

        public override void WriteRow(PositionExportDto item, int rowIndex)
        {
            WriteRowValues(
                item.RowNo,
                item.PositionName,
                item.Description,
                item.UserCount,
                item.PermissionCount,
                item.CreatedDate
            );
        }
    }
}
