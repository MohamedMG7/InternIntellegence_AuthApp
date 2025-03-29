using System.ComponentModel.DataAnnotations;

namespace AuthApp.Dto{
    public class EmailConfirmationDto{
        [Display (Name = "Email Address")]
        public string Email { get; set; } = null!;
        

        [Display (Name = "Verification Code")]
        public string VerificationCode { get; set; } = null!;
    }
}