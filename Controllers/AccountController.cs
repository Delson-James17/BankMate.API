using BankMate.API.Data;
using BankMate.API.Helpers;
using BankMate.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankMate.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context), "AppDbContext cannot be null.");

       private async Task LogTransaction(Guid accountId, decimal amount, string type,string description = "")
        {
            var transaction = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Type = type,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
            await _context.Transactions.AddAsync(transaction);

        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (account == null) return NotFound("User not found.");
            await ActivityLogger.LogAsync(_context, userId, "GetBalance", $"Balance {account.Balance}", $"{user?.FirstName}{user?.LastName}");

            return Ok(new { balance = account.Balance });
        }
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] decimal amount)
        {
            if (amount <= 0) return BadRequest("Deposit amount must be greater than zero.");
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null) return NotFound("User not found.");

            account.Balance += amount;
            account.LastUpdated = DateTime.UtcNow;
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");
            await LogTransaction(account.Id, amount, "Deposit","Deposit");
            await ActivityLogger.LogAsync(_context, userId, "Deposit", $"Deposited {amount}", $"{user.FirstName}{user.LastName}");

            await _context.SaveChangesAsync();
            return Ok(new { message = "Deposit succesful", newBalance = account.Balance });
        }
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] decimal amount)
        {
            if (amount <= 0) return BadRequest("Withdrawal amount must be greater than zero.");
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            var user = await _context.Users.FindAsync(userId);
            if (account == null || user == null) return NotFound("User not found.");

            if (account.Balance < amount) return BadRequest("Insufficient balance.");
            account.Balance -= amount;
            account.LastUpdated = DateTime.UtcNow;
            await LogTransaction(account.Id, amount, "Withdraw", "Withdraw");
            await ActivityLogger.LogAsync(_context, userId, "Withdraw", $"Withdraw {amount}", $"{user.FirstName}{user.LastName}");

            await _context.SaveChangesAsync();
            return Ok(new { message = "Withdrawal successful", newBalance = account.Balance });
        }



    }
}
