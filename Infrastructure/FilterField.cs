using System;

namespace CoKhiThaCo.Infrastructure
{
    public class FilterField
    {
        public string Field { get; set; }
        public string Operator { get; set; } = "eq";
        public string Value { get; set; }
    }
}
