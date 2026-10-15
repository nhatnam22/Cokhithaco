using System.Collections.Generic;
using System.IO;
using CoKhiThaCo.Services.Export.Core;

namespace CoKhiThaCo.Services.Export
{
    public interface IExportService<TRequest, TData>
    {
        IEnumerable<TData> FetchData(TRequest request);
        MemoryStream ExportToExcel(TRequest request);
        MemoryStream ExportToPdf(TRequest request);
    }

    public abstract class BaseExportService<TRequest, TData> : IExportService<TRequest, TData>
    {
        protected abstract IExportBuilder<TData> GetExcelBuilder(TRequest request);
        protected abstract IExportBuilder<TData> GetPdfBuilder(TRequest request);

        public abstract IEnumerable<TData> FetchData(TRequest request);

        public virtual MemoryStream ExportToExcel(TRequest request)
        {
            var data = FetchData(request);
            var builder = GetExcelBuilder(request);
            try
            {
                return builder.Build(data);
            }
            finally
            {
                (builder as IDisposable)?.Dispose();
            }
        }

        public virtual MemoryStream ExportToPdf(TRequest request)
        {
            var data = FetchData(request);
            var builder = GetPdfBuilder(request);
            try
            {
                return builder.Build(data);
            }
            finally
            {
                (builder as IDisposable)?.Dispose();
            }
        }
    }
}
