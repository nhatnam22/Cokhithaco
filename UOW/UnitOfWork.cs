using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Repositories;

namespace CoKhiThaCo.UOW
{
    public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext, IDisposable
    {
        private Dictionary<(Type type, string name), object> _repositories;

        public UnitOfWork(TContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public TContext Context { get; }

        Microsoft.EntityFrameworkCore.DbContext Infrastructure.IUnitOfWork.Context => Context;

        public IRepository<T> GetRepository<T>() where T : class
        {
            if (_repositories == null)
            {
                _repositories = new Dictionary<(Type type, string name), object>();
            }

            var type = typeof(T);
            var name = type.Name;
            if (!_repositories.ContainsKey((type, name)))
            {
                _repositories.Add((type, name), new Repository<TContext, T>(Context));
            }
            return (IRepository<T>)_repositories[(type, name)];
        }

        public int Complete()
        {
            return Context.SaveChanges();
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
