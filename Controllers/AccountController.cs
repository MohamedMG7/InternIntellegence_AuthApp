using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AuthApp.Dto;
using AuthApp.Models;
using AuthApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace AuthApp.Controllers{
    
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase{
        private readonly AccountManagementService _accountManagementService;
        private readonly UserManager<ApplicationUser> _UserManager;

        public AccountController(UserManager<ApplicationUser> UserManager, AccountManagementService accountManagementService)
        {
            _accountManagementService = accountManagementService;
            _UserManager = UserManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterAsync(RegisterDto regData){
            if(!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            var result = await _accountManagementService.RegisterAsync(regData);

            if(result.Result){
                return StatusCode(StatusCodes.Status201Created,"Registered Successfully");
            }

            return StatusCode(StatusCodes.Status400BadRequest,result);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto loginData)
        {
            var result = await _accountManagementService.Login(loginData);
            
            if (result.Result.Succeeded)
            {
                return Ok(new { Token = result.Token });
            }
            if (result.Result.IsLockedOut)
            {
                return StatusCode(403, "Account locked out");
            }
            if (result.Result.IsNotAllowed)
            {
                return StatusCode(403, "Login not allowed");
            }
            return BadRequest(result);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _accountManagementService.Logout();
            return Ok(result);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("Invalid email confirmation link");

            var user = await _UserManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("Invalid email confirmation link");

            var result = await _UserManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return Ok("Email confirmed successfully!");

            return BadRequest("Failed to confirm email");
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordData){
            var changePassword = await _accountManagementService.ResetPassword(resetPasswordData);
            
            //not best practice
            if(changePassword == "Password Changed Correctly"){
                return Ok(changePassword);
            }

            return BadRequest(changePassword);
        }


        [HttpPost("enable-2fa")]
        public async Task<IActionResult> Enable2FA([FromQuery]string email)
        {
            var user = await _UserManager.FindByEmailAsync(email);
            if(user == null){
                return BadRequest("Wrong Email Address");
            }
            await _UserManager.SetTwoFactorEnabledAsync(user, true); 
            return Ok("2FA Is Activated");
        }

        [HttpPost("2FA-Login")]
        public async Task<IActionResult> Login2FA([FromQuery]string email, [FromQuery] string otp){
            if(email == null || otp == null){
                return BadRequest("Wrong email or otp");
            }

            var loginResult = await _accountManagementService.TwoFactorLogin(email,otp);

            if(loginResult.Result.Succeeded){
                return Ok(loginResult);
            }
            
            return BadRequest(loginResult);
        }

    }
}