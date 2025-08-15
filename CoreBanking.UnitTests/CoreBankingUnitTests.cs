namespace CoreBanking.UnitTests;

[Collection("Database collection")]
public class CoreBankingUnitTests(DatabaseFixture fixture)
{
	private readonly DatabaseFixture _fixture = fixture;

	[Fact]
	public void Create_Customer_UnitTest()
	{
		// Arrange
		var services = _fixture.CreateService();
		using var context = new CoreBankingDbContext(_fixture.DbContextOptions);

		var customer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "123A Main St",
			Accounts = []
		};

		// Act
		var result = CoreBankingApi.CreateCustomer(services, customer);

		// Assert
		Assert.NotNull(result);

		// Verify that the customer was added to the database
		var addedCustomer = context.Customers.FirstOrDefault(c => c.Id == customer.Id);

		// Assert that the added customer matches the input customer details
		Assert.NotNull(addedCustomer);
		Assert.Equal(addedCustomer.Name, customer.Name);
		Assert.Equal(addedCustomer.Address, customer.Address);
		Assert.Equal(addedCustomer.Accounts.Count, customer.Accounts.Count);
	}

	[Theory]
	[InlineData(100)]
	[InlineData(200)]
	[InlineData(3000)]
	public void Create_Customer_And_Deposit_UnitTest(decimal depositAmount)
	{
		// Arrange
		var services = _fixture.CreateService();

		using var context = new CoreBankingDbContext(_fixture.DbContextOptions);

		// Create a customer
		var customer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "123A Main St",
			Accounts = []
		};

		// Act
		var customerResult = CoreBankingApi.CreateCustomer(services, customer);

		// Assert
		Assert.NotNull(customerResult);

		// Verify that the customer was added to the database
		var addedCustomer = context.Customers.FirstOrDefault(c => c.Id == customer.Id);
		Assert.NotNull(addedCustomer);

		// Create an account for the customer
		var account = new Account
		{
			Id = Guid.NewGuid(),
			CustomerId = customer.Id,
			Balance = 0
		};

		// Act
		var accountResult = CoreBankingApi.CreateAccounts(services, account);

		// Assert that the account was created successful
		Assert.NotNull(accountResult);

		// Deposit money into the account
		var depositResult = CoreBankingApi.Deposit(services, account.Id, new DepositionRequest{
			Amount = depositAmount 
		});

		// Assert that the deposit was successful
		Assert.NotNull(depositResult);

		// Verify that the account balance was updated
		var updatedAccount = context.Accounts.FirstOrDefault(a => a.Id == account.Id);

		// Assert that the account balance was updated
		Assert.NotNull(updatedAccount);
		Assert.Equal(depositAmount, updatedAccount.Balance);

		// Verify that the transactions was created
		var transactions = context.Transactions.Where(t => t.AccountId == account.Id);

		// Assert that a transaction was created for the deposit
		Assert.NotNull(transactions);
		Assert.Equal(1, transactions.Count());
		Assert.Equal(depositAmount, transactions.First().Amount);
	}
}
