using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecurityApi.Model;

namespace SecurityApi.DbContext
{
    public class SecurityDb : IdentityDbContext<ApplicationUser>
    {
        public SecurityDb(DbContextOptions<SecurityDb> options) : base(options)
        {
        }
        public DbSet<RefreshToken> refreshTokens { get; set; }  
        public DbSet<Blog> Blogs { get; set; }
    }
}
