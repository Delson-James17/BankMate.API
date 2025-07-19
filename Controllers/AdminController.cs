using BankMate.API.Data;
using BankMate.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankMate.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context), "AppDbContext cannot be null.");

        [HttpPost("create-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole(CreateRoleDto dto)
        {
            var existingRole = await _context.Roles.AnyAsync(r => r.Name == dto.Name); //task 1
            if (existingRole) return BadRequest("Role already exists.");//task 2

            var role = new Models.Roles //task 3
            {
                Name = dto.Name,
                Description = dto.Description
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new { role.Id, role.Name, role.Description });

        }
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(AssignRoleDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");
            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null) return NotFound("Role not found.");
            user.RoleId = dto.RoleId;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Role '{role.Name}' assigned to {user.FirstName}{user.LastName}" });

        }
        [HttpDelete("delete-account")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteAccount(Guid accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return NotFound("Account not found.");

            var user = await _context.Users.FindAsync(account.UserId);
            if (user == null) return NotFound("User not found");
            _context.Accounts.Remove(account);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Account deleted successfully." });
        }

        [Authorize(Roles ="Admin")]
        [HttpPost("lock-user/{id}")]
        public async Task<IActionResult>LockUser(Guid id )
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User NotFound");

            user.IsLocked = true;
            await _context.SaveChangesAsync();
            return Ok("User Successfully Locked");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("unlock-user/{id}")]
        public async Task<IActionResult>UnlockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User NotFound");

            user.IsLocked = false;
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();
            return Ok("User Succesfully Unlocked");
        }
        [HttpPost("auto-lock")]
        public async Task AutoLockAccount()
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            var userToLock = await _context.Users
                .Include(u => u.Account)
                .Where(u => !u.IsLocked && (u.LastActivity <= sixMonthsAgo || (u.Account != null && u.Account.Balance == 0 && u.Account.LastUpdated <= oneWeekAgo))).ToListAsync();

            foreach(var user in userToLock)
            {
                user.IsLocked = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}
