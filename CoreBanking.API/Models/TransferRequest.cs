namespace CoreBanking.API.Models;

public class TransferRequest
{
	public string DestinationAccountNumber { get; set; } = default!;
	public decimal Amount { get; set; }
}
