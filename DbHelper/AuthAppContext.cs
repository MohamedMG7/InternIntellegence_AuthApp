using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AuthApp.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthApp.DbHelper{
    public class AuthAppContext : IdentityDbContext<ApplicationUser>{
        public AuthAppContext(DbContextOptions<AuthAppContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole{
                    Id = "1",
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp = "1"
                },new IdentityRole{
                    Id = "2",
                    Name = "Admin",
                    NormalizedName = "Admin",
                    ConcurrencyStamp = "2"
                }
            );


        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

    }
}