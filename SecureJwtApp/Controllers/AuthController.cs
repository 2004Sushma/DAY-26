using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SecureJwtApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureJwtApp.Controllers
{
    [ApiController]

    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser>
            _userManager;

        public AuthController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]

        public async Task<IActionResult>
            Register(LoginModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(
                user,
                "User");

            return Ok("User Registered");
        }

        [HttpPost("login")]

        public async Task<IActionResult>
            Login(LoginModel model)
        {
            var user =
                await _userManager
                .FindByEmailAsync(model.Email);

            if (user == null)
            {
                return Unauthorized();
            }

            var validPassword =
                await _userManager
                .CheckPasswordAsync(
                    user,
                    model.Password);

            if (!validPassword)
            {
                return Unauthorized();
            }

            var roles =
                await _userManager
                .GetRolesAsync(user);

            var authClaims =
                new List<Claim>
                {
                    new Claim(
                        ClaimTypes.Name,
                        user.UserName),

                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid().ToString())
                };

            foreach (var role in roles)
            {
                authClaims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var authSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                    "ThisIsMyVerySecureJwtSecretKey123456789"));

            var token =
                new JwtSecurityToken(
                    issuer: "SecureJwtApp",

                    audience: "SecureJwtAppUsers",

                    expires:
                    DateTime.Now.AddMinutes(15),

                    claims: authClaims,

                    signingCredentials:
                    new SigningCredentials(
                        authSigningKey,
                        SecurityAlgorithms
                        .HmacSha256)
                );

            return Ok(new
            {
                token =
                new JwtSecurityTokenHandler()
                .WriteToken(token),

                expiration = token.ValidTo
            });
        }

        [Authorize(Roles = "Admin")]

        [HttpGet("admin")]

        public IActionResult AdminOnly()
        {
            return Ok(
                "Admin Access Granted");
        }

        [Authorize(Roles = "User")]

        [HttpGet("user")]

        public IActionResult UserOnly()
        {
            return Ok(
                "User Access Granted");
        }
    }
}