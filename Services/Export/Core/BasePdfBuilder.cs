using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoKhiThaCo.Services.Export.Core;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace CoKhiThaCo.Services.Export.Core
{
    /// <summary>
    /// Base PDF builder dung PdfSharp (thay the QuestPDF).
    /// </summary>
    public abstract class BasePdfBuilder<T> : IExportBuilder<T>, IDisposable
    {
        public string DocumentTitle { get; set; } = "Report";
        public string PathFile { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "logo.png");

        protected List<ColumnDefinition> _columns;
        protected PdfDocument _document;
        protected PdfPage _page;
        protected XGraphics _gfx;
        protected double _currentY;
        protected int _currentRow;
        protected XFont _fontTitle;
        protected XFont _fontHeader;
        protected XFont _fontBody;
        protected readonly XColor HeaderColor = XColor.FromArgb(20, 92, 158);
        protected readonly XColor BorderColor = XColors.Black;

        static BasePdfBuilder()
        {
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
        }

        protected BasePdfBuilder()
        {
            _columns = new List<ColumnDefinition>();
            _fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
            _fontHeader = new XFont("Arial", 9, XFontStyleEx.Bold);
            _fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);
        }

        public abstract void DefineHeaders();
        public abstract void WriteRow(T item, int rowIndex);

        public virtual MemoryStream Build(IEnumerable<T> data)
        {
            DefineHeaders();

            _document = new PdfDocument();
            _page = _document.AddPage();
            bool landscape = _columns.Count > 8;
            _page.Width = landscape ? XUnit.FromMillimeter(297) : XUnit.FromMillimeter(210);
            _page.Height = landscape ? XUnit.FromMillimeter(210) : XUnit.FromMillimeter(297);
            _gfx = XGraphics.FromPdfPage(_page);

            double margin = XUnit.FromMillimeter(15).Point;
            double usableWidth = _page.Width.Point - margin * 2;
            double colWidth = usableWidth / Math.Max(_columns.Count, 1);

            // Header
            _currentY = margin;
            if (!string.IsNullOrEmpty(PathFile) && File.Exists(PathFile))
            {
                try
                {
                    var image = XImage.FromFile(PathFile);
                    double imgW = 120;
                    double imgH = image.PointHeight / image.PointWidth * imgW;
                    _gfx.DrawImage(image, margin, _currentY, imgW, imgH);
                    _currentY += Math.Max(imgH, 30) + 8;
                }
                catch
                {
                    // logo optional
                }
            }

            if (!string.IsNullOrEmpty(DocumentTitle))
            {
                var titleWidth = _gfx.MeasureString(DocumentTitle, _fontTitle).Width;
                var titleX = margin + (usableWidth - titleWidth) / 2;
                _gfx.DrawString(DocumentTitle, _fontTitle, XBrushes.Black, new XPoint(titleX, _currentY + 14));
                _currentY += 28;
            }

            _currentY += 4;

            // Table header
            double headerHeight = 24;
            double x = margin;
            for (int i = 0; i < _columns.Count; i++)
            {
                var rect = new XRect(x, _currentY, colWidth, headerHeight);
                _gfx.DrawRectangle(new XSolidBrush(HeaderColor), rect);
                _gfx.DrawRectangle(new XPen(BorderColor, 0.5), rect);
                var header = _columns[i].HeaderText ?? string.Empty;
                var hw = _gfx.MeasureString(header, _fontHeader).Width;
                var hx = x + (colWidth - hw) / 2;
                _gfx.DrawString(header, _fontHeader, XBrushes.White, new XPoint(hx, _currentY + 16));
                x += colWidth;
            }
            _currentY += headerHeight;

            // Rows
            int rowIndex = 0;
            foreach (var item in data)
            {
                EnsurePageSpace(margin, usableWidth, colWidth);
                var rowStartY = _currentY;
                double rowHeight = 20;

                // Buffer row values via WriteRow -> SetCellValue
                _rowValues = new string[_columns.Count];
                _rowRowStartY = rowStartY;
                _rowColWidth = colWidth;
                _rowMargin = margin;

                WriteRow(item, rowIndex);

                x = margin;
                for (int i = 0; i < _columns.Count; i++)
                {
                    var rect = new XRect(x, rowStartY, colWidth, rowHeight);
                    _gfx.DrawRectangle(XBrushes.White, rect);
                    _gfx.DrawRectangle(new XPen(BorderColor, 0.5), rect);
                    var text = _rowValues[i] ?? string.Empty;
                    var align = _columns[i].PdfAlign ?? "center";
                    double tx;
                    if (align == "left")
                        tx = x + 4;
                    else
                    {
                        var tw = _gfx.MeasureString(text, _fontBody).Width;
                        tx = x + (colWidth - tw) / 2;
                    }
                    _gfx.DrawString(text, _fontBody, XBrushes.Black, new XPoint(tx, rowStartY + 14));
                    x += colWidth;
                }

                _currentY = rowStartY + rowHeight;
                rowIndex++;
            }

            // Footer
            var footer = $"Trang 1 / 1  -  {DateTime.Now:dd/MM/yyyy HH:mm}";
            _gfx.DrawString(footer, _fontBody, XBrushes.Gray,
                new XPoint(_page.Width.Point - margin - _gfx.MeasureString(footer, _fontBody).Width, _page.Height.Point - margin / 2));

            var stream = new MemoryStream();
            _document.Save(stream, false);
            _document.Dispose();
            _gfx.Dispose();
            stream.Position = 0;
            return stream;
        }

        private string[] _rowValues;
        private double _rowRowStartY;
        private double _rowColWidth;
        private double _rowMargin;

        private void EnsurePageSpace(double margin, double usableWidth, double colWidth)
        {
            if (_currentY + 30 > _page.Height.Point - margin)
            {
                _page = _document.AddPage();
                _page.Width = XUnit.FromMillimeter(_columns.Count > 8 ? 297 : 210);
                _page.Height = XUnit.FromMillimeter(_columns.Count > 8 ? 210 : 297);
                _gfx.Dispose();
                _gfx = XGraphics.FromPdfPage(_page);
                _currentY = margin;

                // redraw header
                double x = margin;
                double headerHeight = 24;
                for (int i = 0; i < _columns.Count; i++)
                {
                    var rect = new XRect(x, _currentY, colWidth, headerHeight);
                    _gfx.DrawRectangle(new XSolidBrush(HeaderColor), rect);
                    _gfx.DrawRectangle(new XPen(BorderColor, 0.5), rect);
                    var header = _columns[i].HeaderText ?? string.Empty;
                    var hw = _gfx.MeasureString(header, _fontHeader).Width;
                    _gfx.DrawString(header, _fontHeader, XBrushes.White, new XPoint(x + (colWidth - hw) / 2, _currentY + 16));
                    x += colWidth;
                }
                _currentY += headerHeight;
            }
        }

        /// <summary>
        /// Ghi 1 o data vao hang hien tai (dung khi WriteRow duoc goi trong Build).
        /// </summary>
        protected void SetCellValue(int columnIndex, object value, string numberFormat = null)
        {
            if (_rowValues == null || columnIndex < 0 || columnIndex >= _rowValues.Length)
                return;
            _rowValues[columnIndex] = FormatCellValue(value, numberFormat);
        }

        /// <summary>
        /// WriteRow theo pattern goc: goi SetCellValue cho tung cot tu index 0.
        /// </summary>
        protected void WriteRowValues(params object[] values)
        {
            for (int i = 0; i < values.Length && i < _columns.Count; i++)
            {
                SetCellValue(i, values[i], _columns[i].NumberFormat);
            }
        }

        protected virtual string FormatCellValue(object value, string numberFormat)
        {
            if (value == null) return string.Empty;

            if (!string.IsNullOrEmpty(numberFormat) && (value is decimal || value is double || value is float))
            {
                var num = Convert.ToDecimal(value);
                if (numberFormat.Contains(".##") && num % 1 == 0)
                    numberFormat = numberFormat.Replace(".##", "");
                return num.ToString(numberFormat);
            }

            if (value is DateTime dt)
                return string.IsNullOrEmpty(numberFormat) ? dt.ToString("dd/MM/yyyy") : dt.ToString(numberFormat);

            if (value is decimal d && string.IsNullOrEmpty(numberFormat))
                return d.ToString("0.##");

            return value.ToString();
        }

        public void ApplyCommonStyles() { }

        public void Dispose()
        {
            _gfx?.Dispose();
            _document?.Dispose();
        }
    }

    /// <summary>
    /// Font resolver dung font he thong Windows de ho tro tieng Viet.
    /// </summary>
    public class WindowsFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var fileName = "arial.ttf";
            if (isBold && isItalic) fileName = "arialbi.ttf";
            else if (isBold) fileName = "arialbd.ttf";
            else if (isItalic) fileName = "ariali.ttf";
            return new FontResolverInfo(fileName);
        }

        public byte[] GetFont(string faceName)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), faceName);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
