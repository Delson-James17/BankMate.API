namespace BankMate.API.DTOs
{
    public class CreateTransactionDto
    {
        public Guid AccountId { get; set; }
        public string Type { get; set; } = "Deposit"; 
        public decimal Amount { get; set; }

    }
}
