using System;
using Microsoft.EntityFrameworkCore;

namespace CoKhiThaCo.Infrastructure
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> GetRepository<T>() where T : class;
        DbContext Context { get; }
        int Complete();
    }
}
