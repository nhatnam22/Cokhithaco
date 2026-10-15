using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoKhiThaCo.Infrastructure;

namespace CoKhiThaCo.Helpers
{
    public static class SortHelper
    {
        public static IOrderedQueryable<T> ApplySort<T>(
            IQueryable<T> query,
            List<SortField> sorts)
        {
            if (sorts == null || sorts.Count == 0)
                return query as IOrderedQueryable<T> ?? query.OrderBy(x => 0);

            var validSorts = sorts
                .Where(s => !string.IsNullOrWhiteSpace(s.Field))
                .ToList();

            if (validSorts.Count == 0)
                return query as IOrderedQueryable<T> ?? query.OrderBy(x => 0);

            IQueryable<T> currentQuery = query;
            bool isFirst = true;

            foreach (var sort in validSorts)
            {
                var property = typeof(T).GetProperty(
                    sort.Field,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    continue;

                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(parameter, property);
                var lambda = Expression.Lambda(propertyAccess, parameter);

                bool isDescending = string.Equals(sort.Direction, "desc", StringComparison.OrdinalIgnoreCase);

                string methodName;
                if (isFirst)
                {
                    methodName = isDescending ? "OrderByDescending" : "OrderBy";
                }
                else
                {
                    methodName = isDescending ? "ThenByDescending" : "ThenBy";
                }

                currentQuery = CallQueryableMethod<T>(currentQuery, methodName, property.PropertyType, lambda);
                isFirst = false;
            }

            return (IOrderedQueryable<T>)currentQuery;
        }

        private static IQueryable<T> CallQueryableMethod<T>(
            IQueryable<T> source,
            string methodName,
            Type propertyType,
            LambdaExpression lambda)
        {
            var method = typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), propertyType);

            return (IQueryable<T>)method.Invoke(null, new object[] { source, lambda });
        }

        public static List<SortField> ParseSorts(string sorts)
        {
            var result = new List<SortField>();
            if (string.IsNullOrWhiteSpace(sorts))
                return result;

            var items = sorts.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var parts = item.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    result.Add(new SortField
                    {
                        Field = parts[0].Trim(),
                        Direction = parts.Length >= 2 ? parts[1].Trim().ToLower() : "asc"
                    });
                }
            }
            return result;
        }
    }
}
