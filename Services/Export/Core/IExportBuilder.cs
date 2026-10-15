using System.Collections.Generic;
using System.IO;

namespace CoKhiThaCo.Services.Export.Core
{
    public interface IExportBuilder<T>
    {
        string PathFile { get; set; }
        string DocumentTitle { get; set; }

        MemoryStream Build(IEnumerable<T> data);
        void DefineHeaders();
        void WriteRow(T item, int rowIndex);
        void ApplyCommonStyles();
    }

    public class ColumnDefinition
    {
        public string HeaderText { get; set; }
        public double? Width { get; set; }
        public string NumberFormat { get; set; }
        public string PdfAlign { get; set; } = "center";
    }
}
