using BankMate.API.Data;
using BankMate.API.DTOs;
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
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private async Task LogTransaction(Guid accountId, decimal amount, string type, string description = "")
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

        [HttpGet("by-account/{accountId}")]
        public async Task<IActionResult> GetByAccount(Guid accountId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
            if (transactions.Count == 0)
                return NotFound("No transactions found for this account.");
            return Ok(transactions);
        }
        [HttpGet("my-transaction")]
        public async Task<IActionResult> GetMyTransaction()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
                return NotFound("User not found.");

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == account.Id)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new {
                    t.Id,
                    t.Amount,
                    t.Type,
                    t.Description,
                    t.Timestamp
                })
                .ToListAsync();
            await ActivityLogger.LogAsync(_context, userId, "GetMyTransaction", "GetMyTransaction",$"{user?.FirstName} {user?.LastName}");

            if (!transactions.Any())
                return NotFound("No transactions found for this account.");

            return Ok(transactions);
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
        {
            if (dto.Amount <= 0) return BadRequest("Transfer amount must be greater than zero.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user identifier.");
            var user = await _context.Users.FindAsync(userId);
            var fromAccount = await _context.Accounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == dto.FromAccountId && a.UserId == userId);
            if (fromAccount == null) return Unauthorized("Invalid sender account");

            var toAccount = await _context.Accounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == dto.ToAccountId);
            if (toAccount == null) return NotFound("Receiver account not found");

            if (fromAccount.Balance < dto.Amount) return BadRequest("Insufficient funds");

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;
          

            fromAccount.LastUpdated = DateTime.UtcNow;
            toAccount.LastUpdated = DateTime.UtcNow;

            await LogTransaction(fromAccount.Id, dto.Amount, "Transfer - Out", dto.Description);
            await LogTransaction(toAccount.Id, dto.Amount, "Transfer - In", dto.Description);
            await ActivityLogger.LogAsync(_context, userId, "Transfer", $"Transfer money from {fromAccount.User?.FirstName} to {toAccount.User?.FirstName} total of {dto.Amount} pesos", $"{user?.FirstName} {user?.LastName}");
               
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Transfer successful",
                fromBalance = fromAccount.Balance,
                toBalance = toAccount.Balance
            });
        }

    }
}
