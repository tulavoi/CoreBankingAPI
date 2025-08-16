namespace CoreBanking.UnitTests;

[Collection("Database collection")]
public class CustomerUnitTests(DatabaseFixture fixture)
{
	private readonly DatabaseFixture _fixture = fixture;

	[Fact]
	public async Task CreateCustomer_WithValidData_ReturnsOk_AddToDatabase()
	{
		// Arrange
		var services = _fixture.CreateService();
		var newCustomer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "123A Main St",
			Accounts = []
		};

		// Act
		var result = await CoreBankingApi.CreateCustomer(services, newCustomer);

		// Assert
		var okResult = Assert.IsType<Ok<Customer>>(result.Result);

		// Extract the Customer object from the OkResult
		var createdCustomer = okResult.Value;
		Assert.NotNull(createdCustomer);
		Assert.Equal(newCustomer.Name, createdCustomer.Name);
		Assert.Equal(newCustomer.Address, createdCustomer.Address);
		Assert.Equal(newCustomer.Accounts.Count, createdCustomer.Accounts.Count);

		// Verify that the newCustomer was added to the database
		using var context = new CoreBankingDbContext(_fixture.DbContextOptions);
		var customerFromDb = context.Customers.FirstOrDefault(c => c.Id == newCustomer.Id);
		Assert.NotNull(customerFromDb);
		Assert.Equal(customerFromDb.Name, newCustomer.Name);
		Assert.Equal(customerFromDb.Address, newCustomer.Address);
		Assert.Equal(customerFromDb.Accounts.Count, newCustomer.Accounts.Count);
	}

	[Fact]
	public async Task CreateCustomer_WithInvalidData_ReturnsBadRequest()
	{
		// Arrange
		var services = _fixture.CreateService();
		var newCustomer = new Customer
		{
			Id = Guid.NewGuid(),
			Name = string.Empty,
			Address = "123A Main St",
			Accounts = []
		};

		// Act
		var result = await CoreBankingApi.CreateCustomer(services, newCustomer);

		// Assert
		Assert.IsType<BadRequest>(result.Result);
	}
}
