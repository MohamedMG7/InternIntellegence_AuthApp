
using System.Text;
using AuthApp.DbHelper;
using AuthApp.Models;
using AuthApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace AuthApp;

public class Program
{
    public static void Main(string[] args)
    {

         

        var builder = WebApplication.CreateBuilder(args); 

        builder.Services.AddControllers();  
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT")); // map the values from the appsettings to JWT class


        builder.Services.AddTransient<EmailService>();
        builder.Services.AddTransient<PhoneMessagesService>();
        
        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<AuthAppContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(Options => {
            Options.Password.RequiredLength = 8;
            Options.Password.RequireLowercase = true;
            Options.Password.RequireUppercase = true;
            Options.Password.RequireDigit = false;

            Options.SignIn.RequireConfirmedEmail = true;

            Options.Lockout.MaxFailedAccessAttempts = 5;

            Options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<AuthAppContext>().AddDefaultTokenProviders()
        .AddTokenProvider<EmailTokenProvider<ApplicationUser>>("Email")
        .AddTokenProvider<PhoneNumberTokenProvider<ApplicationUser>>("PhoneNumber");

        builder.Services.AddAuthentication(Options => {
            Options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            Options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o => {
            o.RequireHttpsMetadata = false;
            o.SaveToken = false;
            o.TokenValidationParameters = new TokenValidationParameters{
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["JWT:Issuer"],
                ValidAudience = builder.Configuration["JWT:Audiance"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
            };
        });

        builder.Services.AddScoped<AccountManagementService>();


        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Run();
    }
}
