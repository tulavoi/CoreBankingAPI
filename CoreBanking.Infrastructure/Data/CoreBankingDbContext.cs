using CoreBanking.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Data;

public class CoreBankingDbContext : DbContext
{
	public CoreBankingDbContext() { }
	public CoreBankingDbContext(DbContextOptions<CoreBankingDbContext> options) : base(options) { }

	public DbSet<Customer> Customers { get; set; } = default!;
	public DbSet<Account> Accounts { get; set; } = default!;
	public DbSet<Transaction> Transactions { get; set; } = default!;
}
