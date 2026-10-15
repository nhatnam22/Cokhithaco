using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CoKhiThaCo.Infrastructure
{
    public interface IRepository<T> where T : class
    {
        T Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Delete(object id);
        void DeleteRange(List<object> lst);
        void RemoveRange(IEnumerable<T> entities);
        T GetById(object id);
        T GetSingle(Expression<Func<T, bool>> whereCondition, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null);
        ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null);
        ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition, List<SortField> sorts, string[] includes = null);
        ICollection<T> GetAll(Expression<Func<T, bool>> whereCondition, List<SortField> sorts, List<FilterField> filters, string[] includes = null);
        ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] includes = null);
        ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, List<SortField> sorts, string[] includes = null);
        ICollection<T> GetAllPaging(Expression<Func<T, bool>> whereCondition, out int total, int page, int pageSize, List<SortField> sorts, List<FilterField> filters, string[] includes = null);
        int Count(Expression<Func<T, bool>> whereCondition);
        bool Exists(Expression<Func<T, bool>> whereCondition);
    }
}
