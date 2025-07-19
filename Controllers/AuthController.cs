using BankMate.API.Data;
using BankMate.API.DTOs;
using BankMate.API.Helpers;
using BankMate.API.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;


namespace BankMate.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AppDbContext context, JwtTokenGenerator jwt) : ControllerBase
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context), "AppDbContext cannot be null.");
        private readonly JwtTokenGenerator _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt), "JwtTokenGenerator cannot be null.");

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);

            if (userExists) return BadRequest("User already exists with this email.");

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

            if (defaultRole == null)
                return BadRequest("Default role not found.");
            var user = new Models.Users
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = defaultRole.Id
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var account = new Account
            {
                UserId = user.Id,
                Balance = 0, 
                LastUpdated = DateTime.UtcNow
            };
            _context.Accounts.Add(account); 
            await _context.SaveChangesAsync();
            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null) return Unauthorized("Invalid username or password");
            if (user.IsLocked) return Unauthorized("Account is locked.Contact the adminsitrator.");
            if (!BCrypt.Net.BCrypt.Verify(dto.Password,user.PasswordHash))
            {
                user.FailedLoginAttempts += 1;

                if(user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                }
                await _context.SaveChangesAsync();
                return Unauthorized("Invalid Username or Password");
            }
            user.FailedLoginAttempts = 0;
            user.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId!));
            if (user == null)
                return Unauthorized("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest("New password and confirmation do not match");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                message = "Password changed successfully",
                token = token
            });

        }

        [Authorize]
        [HttpPut("update-info")]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateInfoDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId!));

            if (user == null)
                return Unauthorized("User not found");

            user.Email = dto.Email;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    Role = user.Role?.Name
                }
            });
        }
    }
}
