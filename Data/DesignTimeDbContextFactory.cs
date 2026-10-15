using CoKhiThaCo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoKhiThaCo
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MyDbContext>
    {
        public MyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=CoKhiThaCoDb;Integrated Security=True;TrustServerCertificate=True;");
            return new MyDbContext(optionsBuilder.Options);
        }
    }
}
