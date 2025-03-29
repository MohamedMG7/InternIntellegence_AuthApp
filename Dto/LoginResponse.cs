using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;

namespace AuthApp.Dto{
    public class LoginResponse{
        public SignInResult Result { get; set; } = null!;
        public string? Token { get; set; }
    }
}