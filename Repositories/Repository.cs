using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;

namespace CoKhiThaCo.Repositories
{
    public class Repository<TContext, T> : IRepository<T> where TContext : DbContext where T : class
    {
        protected readonly TContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(TContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public T Add(T entity)
        {
            return _dbSet.Add(entity).Entity;
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _dbSet.AddRange(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public void Delete(object id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }

        public void DeleteRange(List<object> lst)
        {
            foreach (var id in lst)
                Delete(id);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public T GetSingle(Expression<Func<T, bool>> whereCondition, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null)
        {
            IQueryable<T> query = _dbSet.Where(whereCondition);
            if (includes != null && includes.Length > 0)
            {
                query = ApplyIncludes(query, includes);
            }
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            return query.AsNoTracking().FirstOrDefault();
        }

        public ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (whereCondition != null)
                query = query.Where(whereCondition);
            if (includes != null && includes.Length > 0)
                query = ApplyIncludes(query, includes);
            if (orderBy != null)
                query = orderBy(query);
            return query.AsNoTracking().ToList();
        }

        public ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition, List<SortField> sorts, string[] includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (whereCondition != null)
                query = query.Where(whereCondition);
            if (includes != null && includes.Length > 0)
                query = ApplyIncludes(query, includes);
            var ordered = SortHelper.ApplySort(query, sorts);
            return ordered.AsNoTracking().ToList();
        }

        public ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition, List<SortField> sorts, List<FilterField> filters, string[] includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (whereCondition != null)
                query = query.Where(whereCondition);
            var filterExpression = FilterHelper.BuildFilter<T>(filters);
            if (filterExpression != null)
                query = query.Where(filterExpression);
            if (includes != null && includes.Length > 0)
                query = ApplyIncludes(query, includes);
            var ordered = SortHelper.ApplySort(query, sorts);
            return ordered.AsNoTracking().ToList();
        }

        public ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null)
        {
            IQueryable<T> query = _dbSet.Where(whereCondition);
            if (orderBy != null)
                query = orderBy(query);
            total = query.Count();
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
            if (includes != null && includes.Length > 0)
                query = ApplyIncludes(query, includes);
            return query.AsNoTracking().ToList();
        }

        public ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, List<SortField> sorts, string[] includes = null)
        {
            IQueryable<T> query = _dbSet.Where(whereCondition);
            var ordered = SortHelper.ApplySort(query, sorts);
            total = ordered.Count();
            var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize);
            if (includes != null && includes.Length > 0)
                paged = ApplyIncludes(paged, includes);
            return paged.AsNoTracking().ToList();
        }

        public ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, List<SortField> sorts, List<FilterField> filters, string[] includes = null)
        {
            IQueryable<T> query = _dbSet.Where(whereCondition);
            var filterExpression = FilterHelper.BuildFilter<T>(filters);
            if (filterExpression != null)
                query = query.Where(filterExpression);
            var ordered = SortHelper.ApplySort(query, sorts);
            total = ordered.Count();
            var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize);
            if (includes != null && includes.Length > 0)
                paged = ApplyIncludes(paged, includes);
            return paged.AsNoTracking().ToList();
        }

        public int Count(Expression<Func<T, bool>> whereCondition)
        {
            return _dbSet.Count(whereCondition);
        }

        public bool Exists(Expression<Func<T, bool>> whereCondition)
        {
            return _dbSet.Any(whereCondition);
        }

        private static IQueryable<T> ApplyIncludes(IQueryable<T> query, string[] includes)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return query;
        }
    }
}
