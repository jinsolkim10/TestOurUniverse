using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TestOurUniverse.Data
{
    public class ApppDbContext :IdentityDbContext
    {
        public ApppDbContext(DbContextOptions<ApppDbContext> options) : base(options)
        { 
        }
    }
}
