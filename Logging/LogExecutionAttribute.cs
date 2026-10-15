using System;

namespace CoKhiThaCo.Logging
{
    /// <summary>
    /// Danh dau method can log khi Logging:Operation:LogAllServiceMethods = false.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogExecutionAttribute : Attribute
    {
    }
}
