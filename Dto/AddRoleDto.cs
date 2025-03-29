using System.ComponentModel.DataAnnotations;

namespace AuthApp.Dto{
    public class AddRoleDto{
        [Required]
        public string userId { get; set; } = null!;
        [Required]
        public string roleName { get; set; } = null!;
    }
}