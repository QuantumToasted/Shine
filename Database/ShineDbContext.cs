using Microsoft.EntityFrameworkCore;

namespace Shine.Database
{
    public sealed class ShineDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            // TODO: What DB provider?
            // builder.UseSqlite("Data Source=./Data/Shine.db");
        }
    }
}