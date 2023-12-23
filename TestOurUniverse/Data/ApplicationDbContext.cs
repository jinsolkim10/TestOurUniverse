using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace TestOurUniverse.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }
        public DbSet<UserInfo> UserInfos { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<TokenInfo> TokenInfos { get; set; }
    }
}
