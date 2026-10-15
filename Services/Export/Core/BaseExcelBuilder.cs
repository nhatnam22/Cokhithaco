using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CoKhiThaCo.Services.Export.Core;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CoKhiThaCo.Services.Export.Core
{
    public abstract class BaseExcelBuilder<T> : IExportBuilder<T>, IDisposable
    {
        protected ExcelPackage _package;
        protected ExcelWorksheet _worksheet;
        protected List<ColumnDefinition> _columns;
        protected int _currentRow;
        protected int _dataStartRow;

        public string PathFile { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "logo.png");
        public string DocumentTitle { get; set; } = "Report";

        protected BaseExcelBuilder()
        {
            _package = new ExcelPackage();
            _columns = new List<ColumnDefinition>();
            _currentRow = 1;
        }

        public abstract void DefineHeaders();
        public abstract void WriteRow(T item, int rowIndex);

        public virtual MemoryStream Build(IEnumerable<T> data)
        {
            _worksheet = _package.Workbook.Worksheets.Add(DocumentTitle);
            _worksheet.Cells.Style.Font.Name = "Calibri";
            _worksheet.Cells.Style.Font.Size = 12;

            DefineHeaders();
            WriteDocumentTitle();
            RenderHeaderRow();

            _dataStartRow = _currentRow;
            int rowIndex = 0;
            foreach (var item in data)
            {
                WriteRow(item, rowIndex);
                _currentRow++;
                rowIndex++;
            }

            ApplyCommonStyles();

            var stream = new MemoryStream();
            _package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        protected virtual void WriteDocumentTitle()
        {
            _worksheet.Row(_currentRow).Height = 50;
            if (!string.IsNullOrEmpty(PathFile) && File.Exists(PathFile))
            {
                var logoFile = new FileInfo(PathFile);
                var picture = _worksheet.Drawings.AddPicture("CompanyLogo", logoFile);
                picture.SetPosition(_currentRow - 1, 5, 0, 10);
                picture.SetSize(140, 60);
            }

            if (string.IsNullOrEmpty(DocumentTitle))
            {
                _currentRow++;
                return;
            }

            _worksheet.Cells[_currentRow, 1].Value = DocumentTitle;
            _worksheet.Cells[_currentRow, 1, _currentRow, _columns.Count].Merge = true;
            _worksheet.Cells[_currentRow, 1].Style.Font.Size = 18;
            _worksheet.Cells[_currentRow, 1].Style.Font.Bold = true;
            _worksheet.Cells[_currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            _currentRow++;
            _currentRow++;
        }

        protected virtual void RenderHeaderRow()
        {
            _worksheet.Row(_currentRow).Height = 25;
            for (int i = 0; i < _columns.Count; i++)
            {
                var cell = _worksheet.Cells[_currentRow, i + 1];
                cell.Value = _columns[i].HeaderText;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(20, 92, 158));
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                if (_columns[i].Width.HasValue)
                    _worksheet.Column(i + 1).Width = _columns[i].Width.Value;
            }
            _currentRow++;
        }

        public virtual void ApplyCommonStyles()
        {
            if (_worksheet.Dimension == null) return;

            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].Width.HasValue)
                    _worksheet.Column(i + 1).AutoFit();
            }

            for (int row = _dataStartRow; row < _currentRow; row++)
            {
                for (int col = 1; col <= _columns.Count; col++)
                {
                    var cell = _worksheet.Cells[row, col];
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
            }
        }

        protected void SetCellValue(int rowIndex, int colIndex, object value, string numberFormat = null)
        {
            var cell = _worksheet.Cells[rowIndex, colIndex];
            cell.Value = value;

            if (!string.IsNullOrEmpty(numberFormat) && value is decimal d)
            {
                cell.Style.Numberformat.Format = numberFormat;
            }
            else if (value is DateTime dt)
            {
                cell.Style.Numberformat.Format = string.IsNullOrEmpty(numberFormat) ? "dd/MM/yyyy" : numberFormat;
            }
            else if (value is decimal || value is double || value is float || value is int || value is long)
            {
                cell.Style.Numberformat.Format = string.IsNullOrEmpty(numberFormat) ? "0.##" : numberFormat;
            }
        }

        public void Dispose()
        {
            _package?.Dispose();
        }
    }
}
