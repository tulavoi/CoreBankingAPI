namespace CoreBanking.UnitTests;

// Fixture class để chia sẻ Context
public class DatabaseFixture : IDisposable
{
	private readonly SqliteConnection _sqlConnection;
	public DbContextOptions<CoreBankingDbContext> DbContextOptions { get; }

	public DatabaseFixture()
	{
		_sqlConnection = new SqliteConnection("DataSource=:memory:");
		_sqlConnection.Open();
		DbContextOptions = new DbContextOptionsBuilder<CoreBankingDbContext>()
			.UseSqlite(_sqlConnection)
			.Options;

		using var context = new CoreBankingDbContext(DbContextOptions);
		context.Database.EnsureCreated();
	}

	public CoreBankingServices CreateService()
	{
		var context = new CoreBankingDbContext(DbContextOptions);
		return new CoreBankingServices(context, NullLogger<CoreBankingServices>.Instance);
	}

	public void Dispose()
	{
		_sqlConnection.Dispose();
	}
}

// Sử dụng CollectionDefinition để chia sẻ Fixture
[CollectionDefinition("Database collection")]
public class DatabaseCollection: ICollectionFixture<DatabaseFixture>
{
	// Class này không có code, chỉ dùng để chia sẻ fixture giữa các test class
}