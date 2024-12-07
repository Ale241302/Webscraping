using Microsoft.EntityFrameworkCore;
using WebScraping.Models;

namespace WebScraping.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; } // Agrega esta línea para la entidad User

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("WebScrapingContext");
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}
