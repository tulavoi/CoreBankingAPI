namespace CoreBanking.UnitTests;

[Collection("Database collection")]
public class TransactionUnitTests(DatabaseFixture fixture)
{
	private readonly DatabaseFixture _fixture = fixture;

	[Theory]
	[InlineData(100)]
	[InlineData(200)]
	[InlineData(3000)]
	public async Task Deposit_WithValidAmount_ReturnsOk_UpdatesBalance_CreatesTransaction(decimal depositAmount)
	{
		// Arrange
		var services = _fixture.CreateService();
		var customer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "123A Main St",
			Accounts = []
		};
		var account = new Account
		{
			Id = Guid.NewGuid(),
			CustomerId = customer.Id,
			Balance = 0,
			AccountNumber = DateTime.UtcNow.Ticks.ToString()
		};

		using (var setupContext = new CoreBankingDbContext(_fixture.DbContextOptions))
		{
			setupContext.Customers.Add(customer);
			setupContext.Accounts.Add(account);
			await setupContext.SaveChangesAsync();
		}

		// Act
		var result = await CoreBankingApi.Deposit(services, account.Id, new DepositionRequest
		{
			Amount = depositAmount,
		});

		// Assert
		// Verify the return value of Api call
		var okResult = Assert.IsType<Ok<Account>>(result.Result);
		var returnedAccount = okResult.Value;
		Assert.NotNull(returnedAccount);
		Assert.Equal(depositAmount, returnedAccount.Balance);

		using (var verifyContext = new CoreBankingDbContext(_fixture.DbContextOptions))
		{
			// Verify the account balance was updated
			var updatedAccount = verifyContext.Accounts.FirstOrDefault(a => a.Id == account.Id);
			Assert.NotNull(updatedAccount);
			Assert.Equal(depositAmount, updatedAccount.Balance);

			// Verify that a transaction was created for the deposit
			var transactions = verifyContext.Transactions.Where(t => t.AccountId == account.Id);
			Assert.NotNull(transactions);
			Assert.Equal(1, transactions.Count());
			Assert.Equal(depositAmount, transactions.First().Amount);
		}
	}

	[Theory]
	[InlineData(-100)]
	[InlineData(0)]
	public async Task Deposit_WithInvalidAmount_ReturnsBadRequest_DontUpdatesBalance(decimal depositAmount)
	{
		// Arrange
		var services = _fixture.CreateService();
		var customer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "123A Main St",
			Accounts = []
		};
		int DEFAULT_AMOUNT = 200;
		var account = new Account
		{
			Id = Guid.NewGuid(),
			CustomerId = customer.Id,
			Balance = DEFAULT_AMOUNT,
			AccountNumber = DateTime.UtcNow.Ticks.ToString()
		};

		using (var setupContext = new CoreBankingDbContext(_fixture.DbContextOptions))
		{
			setupContext.Customers.Add(customer);
			setupContext.Accounts.Add(account);
			await setupContext.SaveChangesAsync();
		}

		// Act
		var result = await CoreBankingApi.Deposit(services, account.Id, new DepositionRequest
		{
			Amount = depositAmount,
		});

		// Assert
		Assert.IsType<BadRequest>(result.Result);

		using (var verifyContext = new CoreBankingDbContext(_fixture.DbContextOptions))
		{
			// Verify the account balance was not changed
			var unchangedAccount = verifyContext.Accounts.FirstOrDefault(a => a.Id == account.Id);
			Assert.NotNull(unchangedAccount);
			Assert.Equal(DEFAULT_AMOUNT, unchangedAccount.Balance);
		}
	}
}
