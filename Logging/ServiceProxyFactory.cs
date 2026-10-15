using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoKhiThaCo.Logging
{
    public static class ServiceProxyFactory
    {
        private static readonly ProxyGenerator ProxyGenerator = new();

        /// <summary>
        /// Dang ky service bang Castle DynamicProxy de interceptor log method.
        /// Controller van inject IInterface nhu binh thuong.
        /// </summary>
        public static IServiceCollection AddProxiedScoped<TInterface, TImpl>(this IServiceCollection services)
            where TInterface : class
            where TImpl : class, TInterface
        {
            services.AddScoped<TImpl>();
            services.RemoveAll<TInterface>();
            services.AddScoped<TInterface>(sp =>
            {
                var impl = sp.GetRequiredService<TImpl>();
                var interceptor = sp.GetRequiredService<LoggingInterceptor>();
                return ProxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(impl, interceptor);
            });
            return services;
        }
    }
}
