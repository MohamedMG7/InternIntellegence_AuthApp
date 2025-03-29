using Microsoft.AspNetCore.Identity;

namespace AuthApp.Models{
    public class ApplicationUser : IdentityUser{
        public string ApplicationUserName { get; set; } = null!;
        
    }
}