using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Infrastructure.Data;

public class CoreBankingDbContextFactory : IDesignTimeDbContextFactory<CoreBankingDbContext>
{
	public CoreBankingDbContext CreateDbContext(string[] args)
	{
		var optionBuilder = new DbContextOptionsBuilder<CoreBankingDbContext>();
		optionBuilder.UseNpgsql("Host=localhost;Database=corebanking;Username=postgres;Password=postgres");
		return new CoreBankingDbContext(optionBuilder.Options);
	}
}
