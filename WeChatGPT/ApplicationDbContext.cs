using Microsoft.EntityFrameworkCore;
using WeChatGPT.Models;

namespace WeChatGPT
{
    public class ApplicationDbContext : DbContext
    {
        public static string ConnectionString { get; set; }
        public DbSet<ChatgptRecord> ChatgptRecords { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public ApplicationDbContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var serverVersion = new MySqlServerVersion(new Version(5, 7));
                optionsBuilder.UseMySql(ConnectionString, serverVersion, builder =>
                {
                    builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
            }
            base.OnConfiguring(optionsBuilder);
        }
    }
}
