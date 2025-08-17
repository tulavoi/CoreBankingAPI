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

	[Theory]
	[InlineData(0, 2, "Alice", "Bob")] // First page
	[InlineData(1, 2, "Jotaro", "Takamura")] // Second page
	[InlineData(0, 1, "Alice")] // Page size of 1
	public async Task GetCustomers_WithPagination_ReturnsCorrectly(
			int pageIndex,
			int pageSize,
			params string[] expectedNames
		)
	{
		// Arrange
		var services = _fixture.CreateService();
		var customers = new List<Customer>()
		{
			new() { Id = Guid.NewGuid(), Name = "Jotaro", Address = string.Empty },
			new() { Id = Guid.NewGuid(), Name = "Alice", Address = string.Empty },
			new() { Id = Guid.NewGuid(), Name = "Takamura", Address = string.Empty },
			new() { Id = Guid.NewGuid(), Name = "Bob", Address = string.Empty },
		};

		// Seed Data
		using var context = new CoreBankingDbContext(_fixture.DbContextOptions);
		// Delete customers in context before adding new customers
		context.Customers.RemoveRange(context.Customers);
		await context.SaveChangesAsync();
		context.Customers.AddRange(customers);
		await context.SaveChangesAsync();

		var pagination = new PaginationRequest
		{
			PageIndex = pageIndex,
			PageSize = pageSize
		};

		// Act
		var result = await CoreBankingApi.GetCustomers(services, pagination);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Value);
		Assert.Equal(pageIndex, result.Value.PageIndex);
		Assert.Equal(pageSize, result.Value.PageSize);
		Assert.Equal(customers.Count, result.Value.Count);
		Assert.Equal(expectedNames.Count(), result.Value.Items.Count());
		Assert.Equal(expectedNames.ToList(), result.Value.Items.Select(c => c.Name).ToList());
	}
}
