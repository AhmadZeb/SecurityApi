using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecurityApi.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecurityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _manager;
        private readonly IConfiguration _config;


        public SecurityController(UserManager<ApplicationUser> manager, IConfiguration config)
        {
            _manager = manager;
            _config = config;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid data provided", Errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            // Check Email and Username in parallel
            var userEmail = await _manager.FindByEmailAsync(model.Email);
            var userName = await _manager.FindByNameAsync(model.UserName);

            if (userEmail is not null)
                return Conflict(new { Message = "Email is already used!" });

            if (userName is not null)
                return Conflict(new { Message = "Username is already used!" });

            // Create the user
            var result = await _manager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully" });
            }

            // Return detailed errors if creation fails
            return BadRequest(new { Message = "User registration failed", Errors = result.Errors });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            var user = await _manager.FindByNameAsync(model.UserName);

            if (user == null || !await _manager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized("Invalid login credentials.");
            }

            var jwtToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken);
            await _manager.UpdateAsync(user);

            return Ok(new
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                UserName= user.UserName,
                UserEmail= user.Email
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RequestToken model)
        {
            var user = await _manager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == model.RefreshToken));

            if (user == null)
            {
                return Unauthorized("Invalid refresh token.");
            }

            var refreshToken = user.RefreshTokens.SingleOrDefault(t =>
                t.Token == model.RefreshToken && t.ExpiryDate >= DateTime.UtcNow);

            if (refreshToken == null)
            {
                return Unauthorized("Refresh token not found or has expired.");
            }

            if (refreshToken.IsUsed || refreshToken.IsRevoked)
            {
                return Unauthorized("Refresh token has already been used or revoked.");
            }

            refreshToken.IsUsed = true;
            await _manager.UpdateAsync(user);

            var newJwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(newRefreshToken);
            await _manager.UpdateAsync(user);

            return Ok(new
            {
                Token = newJwtToken,
                RefreshToken = newRefreshToken.Token
            });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Access Token expiry (e.g. 15 mins)
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken()
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiryDate = DateTime.UtcNow.AddDays(7)// Refresh Token expiry (e.g. 7 days)
            };
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            var user = await _manager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return BadRequest(new { Message = "User with the specified email does not exist." });
            }

            var resetToken = await _manager.GeneratePasswordResetTokenAsync(user);

            // Simulate sending the token to the user via email.
            // In a real scenario, you would use an email service to send the token.
            return Ok(new
            {
                Message = "Password reset link has been sent to the provided email.",
                ResetToken = resetToken // Include for testing; remove in production
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            var user = await _manager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return BadRequest(new { Message = "User with the specified email does not exist." });
            }

            var token = await _manager.GeneratePasswordResetTokenAsync(user);
            // Use the reset verification code directly, no decoding necessary
            var result = await _manager.ResetPasswordAsync(user, token, model.NewPassword);

           

            if (result.Succeeded)
            {
                return Ok(new { Message = "Password has been reset successfully." });
            }

            return BadRequest(result.Errors);
        }

    }
}
