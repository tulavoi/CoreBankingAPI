using CoreBanking.Infrastructure.Data;
using CoreBanking.MigrationService;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

//builder.AddNpgsqlDbContext<CoreBankingDbContext>("corebanking-db", configureDbContextOptions: dbContextOptionBuilder =>
//{
//	dbContextOptionBuilder.useNpgsql(builder => builder.MigrationsAssembly(typeof(CoreBankingDbContext).Assembly.FullName));
//});

builder.Services.AddDbContext<CoreBankingDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("corebanking-db"),
		npgsqlOptions =>
		{
			npgsqlOptions.MigrationsAssembly(typeof(CoreBankingDbContext).Assembly.FullName);
		});
});

var host = builder.Build();
host.Run();
