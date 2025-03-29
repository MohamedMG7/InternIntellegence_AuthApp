using System.ComponentModel.DataAnnotations;

namespace AuthApp.Dto{
    public class ResetPasswordDto{
        [Required]
        public string email { get; set; } = null!;

        public string CurrentPassword { get; set; } = null!;

        public string NewPassword { get; set; } = null!;
    }
}