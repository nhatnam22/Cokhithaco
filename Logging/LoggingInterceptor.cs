using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoKhiThaCo.Logging
{
    /// <summary>
    /// Castle DynamicProxy interceptor - log truoc/sau method cua I*Service.
    /// Chi log ra terminal, khong luu DB.
    /// </summary>
    public class LoggingInterceptor : IInterceptor
    {
        private static readonly MethodInfo InterceptGenericMethod =
            typeof(LoggingInterceptor).GetMethod(nameof(InterceptAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly ILogger<LoggingInterceptor> _logger;
        private readonly LoggingOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggingInterceptor(
            ILogger<LoggingInterceptor> logger,
            IOptions<LoggingOptions> options,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _options = options.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Intercept(IInvocation invocation)
        {
            var typeName = invocation.TargetType.Name;
            var methodName = invocation.Method.Name;

            if (!_options.LogAllServiceMethods && !HasLogAttribute(invocation.Method))
            {
                invocation.Proceed();
                return;
            }

            var args = OperationLogFormat.FormatArgs(invocation.Arguments, _options);
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = OperationLogFormat.UserId(user);

            _logger.LogInformation(
                "[SVC] -> {Type}.{Method}{Args} | user={UserId}",
                typeName,
                methodName,
                args,
                userId);

            var sw = Stopwatch.StartNew();

            try
            {
                invocation.Proceed();

                if (invocation.ReturnValue is Task task)
                {
                    var returnType = invocation.Method.ReturnType;

                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var resultType = returnType.GetGenericArguments()[0];
                        invocation.ReturnValue = InterceptGenericMethod
                            .MakeGenericMethod(resultType)
                            .Invoke(this, new object[] { task, typeName, methodName, sw });
                    }
                    else
                    {
                        invocation.ReturnValue = InterceptAsync(task, typeName, methodName, sw);
                    }
                    return;
                }

                sw.Stop();
                _logger.LogInformation(
                    "[SVC] <- {Type}.{Method} OK {Ms}ms",
                    typeName,
                    methodName,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogFail(typeName, methodName, sw, ex);
                throw;
            }
        }

        private async Task InterceptAsync(Task task, string typeName, string methodName, Stopwatch sw)
        {
            try
            {
                await task.ConfigureAwait(false);
                sw.Stop();
                _logger.LogInformation(
                    "[SVC] <- {Type}.{Method} OK {Ms}ms",
                    typeName,
                    methodName,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogFail(typeName, methodName, sw, ex);
                throw;
            }
        }

        private async Task<T> InterceptAsyncGeneric<T>(Task<T> task, string typeName, string methodName, Stopwatch sw)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                sw.Stop();
                _logger.LogInformation(
                    "[SVC] <- {Type}.{Method} OK {Ms}ms",
                    typeName,
                    methodName,
                    sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogFail(typeName, methodName, sw, ex);
                throw;
            }
        }

        private void LogFail(string typeName, string methodName, Stopwatch sw, Exception ex)
        {
            _logger.LogError(
                ex,
                "[SVC] <- {Type}.{Method} FAIL {Ms}ms: {ExType}: {Msg}",
                typeName,
                methodName,
                sw.ElapsedMilliseconds,
                ex.GetType().Name,
                ex.Message);
        }

        private static bool HasLogAttribute(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(LogExecutionAttribute), true).Length > 0;
        }
    }
}
