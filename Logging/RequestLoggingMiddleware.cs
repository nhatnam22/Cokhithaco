using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoKhiThaCo.Logging
{
    /// <summary>
    /// Log HTTP request truoc/sau khi pipeline xu ly. Chi log ra terminal.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly LoggingOptions _options;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger,
            IOptions<LoggingOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.LogHttpRequest || ShouldSkip(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var correlationId = GetOrGenerateCorrelationId(context);
            var method = context.Request.Method;
            var path = context.Request.Path + context.Request.QueryString;
            var user = context.User;

            _logger.LogInformation(
                "[HTTP] -> {Method} {Path} | user={User} ({UserId}) | role={Role} | trace={Trace}",
                method,
                path,
                OperationLogFormat.UserName(user),
                OperationLogFormat.UserId(user),
                OperationLogFormat.GlobalRole(user),
                correlationId);

            var sw = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                var status = context.Response.StatusCode;

                if (exception != null)
                {
                    _logger.LogError(
                        exception,
                        "[HTTP] <- {Status} {Method} {Path} | {Ms}ms | {ExType}: {Msg} | trace={Trace}",
                        status,
                        method,
                        path,
                        sw.ElapsedMilliseconds,
                        exception.GetType().Name,
                        exception.Message,
                        correlationId);
                }
                else if (status >= 400)
                {
                    _logger.LogWarning(
                        "[HTTP] <- {Status} {Method} {Path} | {Ms}ms | trace={Trace}",
                        status,
                        method,
                        path,
                        sw.ElapsedMilliseconds,
                        correlationId);
                }
                else
                {
                    _logger.LogInformation(
                        "[HTTP] <- {Status} {Method} {Path} | {Ms}ms | trace={Trace}",
                        status,
                        method,
                        path,
                        sw.ElapsedMilliseconds,
                        correlationId);
                }
            }
        }

        private bool ShouldSkip(PathString path)
        {
            var value = path.Value ?? string.Empty;
            foreach (var skip in _options.SkipPaths)
            {
                if (!string.IsNullOrEmpty(skip) && value.StartsWith(skip, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string GetOrGenerateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing.ToString();
            }

            var id = Guid.NewGuid().ToString("N")[..12];
            context.Response.Headers["X-Correlation-Id"] = id;
            return id;
        }
    }
}
