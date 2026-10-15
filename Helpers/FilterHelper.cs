using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoKhiThaCo.Infrastructure;

namespace CoKhiThaCo.Helpers
{
    public static class FilterHelper
    {
        public static Expression<Func<T, bool>> BuildFilter<T>(List<FilterField> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression finalExpression = null;

            var grouped = filters
                .Where(f => !string.IsNullOrWhiteSpace(f.Field))
                .GroupBy(f => new { f.Field, f.Operator });

            foreach (var group in grouped)
            {
                var fieldName = group.Key.Field;
                var op = group.Key.Operator?.ToLower() ?? "eq";
                var rawValues = group.Select(f => f.Value).ToList();
                var values = ExtractValues(rawValues);

                var property = typeof(T).GetProperty(
                    fieldName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    continue;

                var propertyAccess = Expression.Property(parameter, property);
                Expression filterExpression = null;

                if (op == "isnull")
                {
                    filterExpression = Expression.Equal(propertyAccess, Expression.Constant(null, property.PropertyType));
                }
                else if (op == "isnotnull")
                {
                    filterExpression = Expression.NotEqual(propertyAccess, Expression.Constant(null, property.PropertyType));
                }
                else if (values.Count > 1 || op == "in" || op == "nin")
                {
                    filterExpression = BuildInExpression(propertyAccess, property.PropertyType, op, values);
                }
                else if (values.Count == 1)
                {
                    filterExpression = BuildSingleExpression(propertyAccess, property, op, values[0]);
                }

                if (filterExpression == null)
                    continue;

                finalExpression = finalExpression == null
                    ? filterExpression
                    : Expression.AndAlso(finalExpression, filterExpression);
            }

            return finalExpression == null
                ? null
                : Expression.Lambda<Func<T, bool>>(finalExpression, parameter);
        }

        private static List<string> ExtractValues(List<string> rawValues)
        {
            var result = new List<string>();
            foreach (var val in rawValues)
            {
                if (string.IsNullOrWhiteSpace(val)) continue;

                var trimmed = val.Trim();
                if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
                {
                    trimmed = trimmed.Substring(1, trimmed.Length - 2);
                }

                var split = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries);
                result.AddRange(split.Select(s => s.Trim()));
            }
            return result;
        }

        private static Expression BuildSingleExpression(
            Expression propertyAccess,
            PropertyInfo property,
            string op,
            string value)
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (string.IsNullOrEmpty(value))
                return null;

            if (underlyingType == typeof(string))
                return BuildStringExpression(propertyAccess, op, value);

            if (underlyingType == typeof(bool))
                return BuildBoolExpression(propertyAccess, propertyType, value);

            if (underlyingType == typeof(DateTime))
                return BuildDateTimeExpression(propertyAccess, propertyType, op, value);

            if (underlyingType == typeof(Guid))
                return BuildGuidExpression(propertyAccess, propertyType, op, value);

            if (IsNumericType(underlyingType))
                return BuildNumericExpression(propertyAccess, propertyType, underlyingType, op, value);

            if (underlyingType.IsEnum)
                return BuildEnumExpression(propertyAccess, propertyType, underlyingType, op, value);

            return null;
        }

        private static Expression BuildInExpression(Expression propertyAccess, Type propertyType, string op, List<string> values)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var nonEmptyValues = values.Where(v => !string.IsNullOrEmpty(v)).ToList();
            if (nonEmptyValues.Count == 0)
                return null;

            var inner = BuildInExpressionCore(propertyAccess, propertyType, underlyingType, nonEmptyValues);
            if (inner == null) return null;

            return op == "neq" || op == "nin" ? Expression.Not(inner) : inner;
        }

        private static Expression BuildInExpressionCore(Expression propertyAccess, Type propertyType, Type underlyingType, List<string> values)
        {
            IList typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyType));

            foreach (var v in values)
            {
                var convertedVal = ConvertValue(v, underlyingType);
                if (convertedVal != null)
                {
                    typedList.Add(convertedVal);
                }
            }

            if (typedList.Count == 0)
                return null;

            var listConstant = Expression.Constant(typedList);
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(propertyType);

            return Expression.Call(containsMethod, listConstant, propertyAccess);
        }

        private static object ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType == typeof(string)) return value;
                if (targetType == typeof(Guid) && Guid.TryParse(value, out var g)) return g;
                if (targetType == typeof(bool)) return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                if (targetType == typeof(DateTime) && DateTime.TryParse(value, out var dt)) return dt;
                if (targetType.IsEnum)
                {
                    if (int.TryParse(value, out var intVal)) return Enum.ToObject(targetType, intVal);
                    if (Enum.TryParse(targetType, value, true, out var enumObj)) return enumObj;
                    return null;
                }
                if (IsNumericType(targetType))
                {
                    return Convert.ChangeType(value, targetType);
                }
            }
            catch { }
            return null;
        }

        private static Expression BuildStringExpression(Expression propertyAccess, string op, string value)
        {
            var constant = Expression.Constant(value, typeof(string));
            var notNullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));

            Expression body = op switch
            {
                "eq" => Expression.Equal(propertyAccess, constant),
                "neq" => Expression.NotEqual(propertyAccess, constant),
                "contains" => Expression.Call(propertyAccess, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, constant),
                "notcontains" => Expression.Not(Expression.Call(propertyAccess, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, constant)),
                "startswith" => Expression.Call(propertyAccess, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, constant),
                "endswith" => Expression.Call(propertyAccess, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, constant),
                _ => Expression.Equal(propertyAccess, constant)
            };

            return (op == "neq" || op == "notcontains") ? body : Expression.AndAlso(notNullCheck, body);
        }

        private static Expression BuildBoolExpression(Expression propertyAccess, Type propertyType, string value)
        {
            bool boolValue = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            var constant = Expression.Convert(Expression.Constant(boolValue), propertyType);
            return Expression.Equal(propertyAccess, constant);
        }

        private static Expression BuildDateTimeExpression(Expression propertyAccess, Type propertyType, string op, string value)
        {
            if (!DateTime.TryParse(value, out var dateValue))
                return null;

            var constant = Expression.Convert(Expression.Constant(dateValue), propertyType);
            return CombineComparison(propertyAccess, constant, op);
        }

        private static Expression BuildGuidExpression(Expression propertyAccess, Type propertyType, string op, string value)
        {
            if (!Guid.TryParse(value, out var guidValue))
                return null;

            var constant = Expression.Convert(Expression.Constant(guidValue), propertyType);
            return op == "neq" ? Expression.NotEqual(propertyAccess, constant) : Expression.Equal(propertyAccess, constant);
        }

        private static Expression BuildNumericExpression(Expression propertyAccess, Type propertyType, Type underlyingType, string op, string value)
        {
            var val = ConvertValue(value, underlyingType);
            if (val == null) return null;

            var constant = Expression.Convert(Expression.Constant(val, underlyingType), propertyType);
            return CombineComparison(propertyAccess, constant, op);
        }

        private static Expression BuildEnumExpression(Expression propertyAccess, Type propertyType, Type underlyingType, string op, string value)
        {
            var enumVal = ConvertValue(value, underlyingType);
            if (enumVal == null) return null;

            var constant = Expression.Convert(Expression.Constant(enumVal, underlyingType), propertyType);
            return op == "neq" ? Expression.NotEqual(propertyAccess, constant) : Expression.Equal(propertyAccess, constant);
        }

        private static Expression CombineComparison(Expression left, Expression right, string op)
        {
            return op switch
            {
                "eq" => Expression.Equal(left, right),
                "neq" => Expression.NotEqual(left, right),
                "gt" => Expression.GreaterThan(left, right),
                "gte" => Expression.GreaterThanOrEqual(left, right),
                "lt" => Expression.LessThan(left, right),
                "lte" => Expression.LessThanOrEqual(left, right),
                _ => Expression.Equal(left, right)
            };
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(decimal) ||
                   type == typeof(double) || type == typeof(float) || type == typeof(short) ||
                   type == typeof(byte);
        }

        public static List<FilterField> ParseFilters(string filters)
        {
            var result = new List<FilterField>();
            if (string.IsNullOrWhiteSpace(filters))
                return result;

            var items = SplitFilterItems(filters);
            foreach (var item in items)
            {
                var parts = item.Split(new[] { ':' }, 3);
                if (parts.Length < 2)
                    continue;

                var field = parts[0].Trim();
                var op = parts[1].Trim().ToLower();
                var value = parts.Length >= 3 ? parts[2].Trim() : null;

                if (string.IsNullOrWhiteSpace(field))
                    continue;

                result.Add(new FilterField
                {
                    Field = field,
                    Operator = op,
                    Value = value
                });
            }
            return result;
        }

        private static List<string> SplitFilterItems(string filters)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(filters))
                return result;

            int depth = 0;
            int start = 0;

            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i] == '(') depth++;
                else if (filters[i] == ')') depth--;
                else if (filters[i] == ',' && depth == 0)
                {
                    var item = filters.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(item))
                        result.Add(item);
                    start = i + 1;
                }
            }

            var last = filters.Substring(start).Trim();
            if (!string.IsNullOrEmpty(last))
                result.Add(last);

            return result;
        }
    }
}
