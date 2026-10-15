using System.Collections.Generic;

namespace CoKhiThaCo.Logging
{
    public class LoggingOptions
    {
        public const string SectionName = "Logging:Operation";

        public bool LogHttpRequest { get; set; } = true;
        public bool LogAllServiceMethods { get; set; } = true;
        public int MaxArgLength { get; set; } = 200;
        public List<string> SkipPaths { get; set; } = new()
        {
            "/swagger",
            "/hubs"
        };
        public List<string> SensitiveProperties { get; set; } = new()
        {
            "Password",
            "OldPassword",
            "NewPassword",
            "Token",
            "Secret"
        };
    }
}
