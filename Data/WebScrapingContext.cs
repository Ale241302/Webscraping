using Microsoft.EntityFrameworkCore;
using WebScraping.Models;

namespace WebScraping.Data
{
    public class WebScrapingContext : DbContext
    {
        public WebScrapingContext(DbContextOptions<WebScrapingContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } // Tabla de usuarios
    }
}
