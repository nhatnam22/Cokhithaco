using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CoKhiThaCo.Logging
{
    public static class OperationLogFormat
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        public static string FormatArgs(IReadOnlyList<object> args, LoggingOptions options)
        {
            if (args == null || args.Count == 0)
                return "()";

            var parts = new List<string>();
            foreach (var arg in args)
            {
                parts.Add(FormatValue(arg, options));
            }
            return "(" + string.Join(", ", parts) + ")";
        }

        public static string FormatValue(object value, LoggingOptions options)
        {
            if (value == null) return "null";

            if (value is string s)
                return Truncate("\"" + s + "\"", options?.MaxArgLength ?? 200);

            if (IsSimple(value))
                return value.ToString();

            try
            {
                var json = JsonConvert.SerializeObject(value, JsonSettings);
                json = MaskSensitive(json, options);
                return Truncate(json, options?.MaxArgLength ?? 200);
            }
            catch
            {
                return Truncate(value.ToString(), options?.MaxArgLength ?? 200);
            }
        }

        public static string MaskSensitive(string json, LoggingOptions options)
        {
            if (string.IsNullOrEmpty(json) || options?.SensitiveProperties == null)
                return json;

            foreach (var prop in options.SensitiveProperties)
            {
                // "Password":"xxx" -> "Password":"***"
                json = System.Text.RegularExpressions.Regex.Replace(
                    json,
                    $"(\"{prop}\"\\s*:\\s*)\"[^\"]*\"",
                    "$1\"***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return json;
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value ?? string.Empty;
            return value.Substring(0, maxLength) + "...";
        }

        public static string UserId(System.Security.Claims.ClaimsPrincipal user)
        {
            var claim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim?.Value ?? "-";
        }

        public static string UserName(System.Security.Claims.ClaimsPrincipal user)
        {
            var name = user?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? user?.Identity?.Name;
            return string.IsNullOrEmpty(name) ? "-" : name;
        }

        public static string GlobalRole(System.Security.Claims.ClaimsPrincipal user)
        {
            return user?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "-";
        }

        private static bool IsSimple(object value)
        {
            return value is bool || value is byte || value is sbyte
                || value is short || value is ushort
                || value is int || value is uint
                || value is long || value is ulong
                || value is float || value is double || value is decimal
                || value is DateTime || value is DateTimeOffset || value is TimeSpan
                || value is Guid || value is Enum;
        }
    }
}
