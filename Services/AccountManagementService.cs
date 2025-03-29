using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Xml.Schema;
using AuthApp.DbHelper;
using AuthApp.Dto;
using AuthApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace AuthApp.Services{
    public class AccountManagementService{

        private readonly AuthAppContext _Context;
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly SignInManager<ApplicationUser> _SigninManager;
        private readonly RoleManager<IdentityRole> _RoleManager;
        private readonly EmailService _emailService;
        private readonly PhoneMessagesService _phoneService;
        private readonly JWT _jwt;

        
        private readonly IConfiguration _Configuration;
        public AccountManagementService(PhoneMessagesService phoneService,EmailService emailService,RoleManager<IdentityRole> RoleManager,IOptions<JWT> jwt,IConfiguration Configuration ,AuthAppContext Context, UserManager<ApplicationUser> UserManager,SignInManager<ApplicationUser> SignInManager)
        {
            _Context = Context;
            _UserManager = UserManager;
            _SigninManager = SignInManager;
            _Configuration = Configuration;
            _jwt = jwt.Value;
            _RoleManager = RoleManager;
            _emailService = emailService;
            _phoneService = phoneService;
        }
        public async Task<GeneralResponse> RegisterAsync(RegisterDto RegistrationData){

            var user = new ApplicationUser{
                ApplicationUserName = RegistrationData.Name,
                UserName = RegistrationData.Name,
                Email = RegistrationData.Email
            };

            var AccountRegResult = await _UserManager.CreateAsync(user,RegistrationData.Password);

            if(!AccountRegResult.Succeeded){
                return new GeneralResponse 
                {
                    Result = false,
                    Errors = AccountRegResult.Errors.Select(e => e.Description).ToList()
                };
            }

            await _UserManager.AddToRoleAsync(user, RegistrationData.Role);   
            
            await ConfirmEmailAsync(user);

            return new GeneralResponse {
                Result = AccountRegResult.Succeeded,
                Errors = AccountRegResult.Succeeded ? null : AccountRegResult.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<LoginResponse> Login(LoginDto LoginData)
        {
            var user = await _UserManager.FindByEmailAsync(LoginData.Email);
            if (user == null)
            {
                return new LoginResponse{
                    Result = SignInResult.Failed,
                    Token = null
                };
            }

            var Loginresult = await _SigninManager.PasswordSignInAsync(
                user,
                LoginData.Password,
                isPersistent: false,  
                lockoutOnFailure: true  
            );

            // Generate token only for successful logins
            
            JwtSecurityToken token = new JwtSecurityToken();
            if(Loginresult.Succeeded){
                token = await CreateJwtToken(user);
            }
            
            if(await _UserManager.GetTwoFactorEnabledAsync(user)){ // if the user enabled the 2FA
                await GenerateOTPfor2FA(user);

                return new LoginResponse{
                    Result = SignInResult.Success,
                    Token = "Code Sent To Email"
                };
            }

            return new LoginResponse{
                Result = Loginresult,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }

        public async Task<LoginResponse> TwoFactorLogin(string email, string otp){
            var user = await _UserManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new LoginResponse
                {
                    Result = SignInResult.Failed,
                    Token = null
                };
            }
            var isValid = await _UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, otp);

            if (!isValid)
            {
                return new LoginResponse
                {
                    Result = SignInResult.Failed,
                    Token = "Invalid 2FA Code"
                };
            }

            var token = await CreateJwtToken(user);
            return new LoginResponse
            {
                Result = SignInResult.Success,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }

        public async Task<string> Logout()
        {
            try
            {
                await _SigninManager.SignOutAsync();
                return "Signed Out";
            }
            catch (Exception ex)
            {
                throw new Exception("Logout failed", ex);
            }
        }


        public async Task<bool> ConfirmEmailAsync(ApplicationUser user){
            var emailAddress = user.Email;
            var Subject = "Email Confirmation";
            var ConfirmationToken = await _UserManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(ConfirmationToken); // should be encoded because it has special characters
            var confirmationLink = $"{_Configuration["AppUrl"]}/api/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";
            var body = GetEmailTemplate(confirmationLink);

            await _emailService.SendEmailAsync(emailAddress,Subject,body);

            return true;
        }

        private string GetEmailTemplate(string confirmationLink)
        {
            return @$"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 20px auto; background: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <h2 style='color: #333; text-align: center;'>Welcome!</h2>
                    <p style='color: #666; text-align: center;'>Please confirm your email address to complete your registration.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{confirmationLink}' 
                        style='background-color: #4CAF50; 
                                color: white; 
                                padding: 15px 30px; 
                                text-decoration: none; 
                                border-radius: 5px; 
                                font-weight: bold;
                                display: inline-block;'>
                            Confirm Email
                        </a>
                    </div>
                </div>
            </body>
            </html>";
        }

        public async Task<string> ResetPassword(ResetPasswordDto ResetPasswordData){
            
            var user = await _UserManager.FindByEmailAsync(ResetPasswordData.email);
            
            if(user == null){
                return "Email Is Wrong";
            }

            if(!await _UserManager.CheckPasswordAsync(user,ResetPasswordData.CurrentPassword)){
                return "Wrong Passsword";
            }

            if(ResetPasswordData.CurrentPassword == ResetPasswordData.NewPassword){
                return "The New Password Can not Be the Same As Current Password";
            }

            var change = await _UserManager.ChangePasswordAsync(user,ResetPasswordData.CurrentPassword,ResetPasswordData.NewPassword);


            return "Password Changed Correctly";
        }

        //2FA Auth

        


        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user){
            var userClaims = await _UserManager.GetClaimsAsync(user); 
            var roles = await _UserManager.GetRolesAsync(user);

            var roleClaims = new List<Claim>();

            foreach(var role in roles){
                roleClaims.Add(new Claim("roles",role));
            }

            var claims = new[]{
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Uid",user.Id)
            }.Union(userClaims).Union(roleClaims);

            var SymmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var SigningCredentials = new SigningCredentials(SymmetricSecurityKey,SecurityAlgorithms.HmacSha256);

            var JwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audiance,
                claims: claims,
                expires: DateTime.Now.AddHours(_jwt.DurationInHours),
                signingCredentials: SigningCredentials
            );

            return JwtSecurityToken;
        }


        private async Task<string> GenerateOTPfor2FA(ApplicationUser user){
            var Providers = await _UserManager.GetValidTwoFactorProvidersAsync(user);

            if(Providers.Contains("Email") || Providers.Contains("PhoneNumber")){
                var token = await _UserManager.GenerateTwoFactorTokenAsync(user,"Email");
                var Subject = "2FA";
                var userEmail = user.Email;
                var body = $"Code: {token}";

                await _emailService.SendEmailAsync(userEmail,Subject,body);
                await _phoneService.SendSMSAsync(user.PhoneNumber,token);
            }

            return "Something Went Wrong";
        }
        
    }
}