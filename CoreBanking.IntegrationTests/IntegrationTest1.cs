using CoreBanking.API.Models;
using Google.Protobuf.Reflection;

namespace CoreBanking.IntegrationTests.Tests;

public class IntegrationTest1
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

	// Instructions:
	// 1. Add a project reference to the target AppHost project, e.g.:
	//
	//    <ItemGroup>
	//        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
	//    </ItemGroup>
	//
	// 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
	//
	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;
		var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CoreBanking_AppHost>(cancellationToken);
		appHost.Services.AddLogging(logging =>
		{
			logging.SetMinimumLevel(LogLevel.Debug);
			// Override the logging filters from the app's configuration
			logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
			logging.AddFilter("Aspire.", LogLevel.Debug);
			// To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
		});
		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});

		await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
		await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

		// Act
		var httpClient = app.CreateHttpClient("corebanking-api");
		await app.ResourceNotifications.WaitForResourceHealthyAsync("corebanking-api", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

		// START TESTING

		// Arrange
		var customer1 = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "John Doe",
			Address = "1A Main St",
			Accounts = []
		};

		var customer2 = new Customer
		{
			Id = Guid.NewGuid(),
			Name = "David Ben",
			Address = "2A Elm St",
			Accounts = []
		};

		// Act
		var response1 = await httpClient.PostAsJsonAsync("api/v1/corebanking/customers", customer1);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

		// Act
		var response2 = await httpClient.PostAsJsonAsync("api/v1/corebanking/customers", customer2);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

		// Arrange
		var account1 = new Account
		{
			Id = Guid.NewGuid(),
			CustomerId = customer1.Id,
			Balance = 100000,
		};

		var account2 = new Account
		{
			Id = Guid.NewGuid(),
			CustomerId = customer2.Id,
			Balance = 200000,
		};

		// Act
		response1 = await httpClient.PostAsJsonAsync("api/v1/corebanking/accounts", account1);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

		// Act
		var getAccount1 = await response1.Content.ReadFromJsonAsync<Account>();

		// Assert
		Assert.NotNull(getAccount1);
		Assert.Equal(account1.Id, getAccount1.Id);
		Assert.Equal(account1.CustomerId, getAccount1.CustomerId);
		Assert.Equal(account1.Balance, getAccount1.Balance);
		Assert.NotEmpty(getAccount1.AccountNumber);

		// Act
		response2 = await httpClient.PostAsJsonAsync("api/v1/corebanking/accounts", account2);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

		// Act
		var getAccount2 = await response2.Content.ReadFromJsonAsync<Account>();

		// Assert
		Assert.NotNull(getAccount2);
		Assert.Equal(account2.Id, getAccount2.Id);
		Assert.Equal(account2.CustomerId, getAccount2.CustomerId);
		Assert.Equal(account2.Balance, getAccount2.Balance);
		Assert.NotEmpty(getAccount2.AccountNumber);

		// Act
		var getResponse1 = await httpClient.GetAsync($"api/v1/corebanking/customers/{customer1.Id}");

		// Assert
		Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);

		// Act
		var getCustomer1 = await getResponse1.Content.ReadFromJsonAsync<Customer>();

		// Assert
		Assert.NotNull(getCustomer1);
		Assert.Equal(customer1.Id, getCustomer1.Id);
		Assert.Equal(customer1.Name, getCustomer1.Name);
		Assert.Equal(customer1.Address, getCustomer1.Address);

		// Act
		var getResponse2 = await httpClient.GetAsync($"api/v1/corebanking/customers/{customer2.Id}");

		// Assert
		Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

		// Act
		var getCustomer2 = await getResponse2.Content.ReadFromJsonAsync<Customer>();

		// Assert
		Assert.NotNull(getCustomer2);
		Assert.Equal(customer2.Id, getCustomer2.Id);
		Assert.Equal(customer2.Name, getCustomer2.Name);
		Assert.Equal(customer2.Address, getCustomer2.Address);

		// Act
		response1 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{getAccount1.AccountNumber}/deposit", new DepositionRequest()
		{
			Amount = 50000
		});

		// Assert
		Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

		// Act
		response1 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{getAccount2.AccountNumber}/withdraw", new WithdrawalRequest()
		{
			Amount = 500000
		});

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

		// Act
		response1 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{getAccount2.AccountNumber}/withdraw", new WithdrawalRequest()
		{
			Amount = 50000
		});

		// Assert
		Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

		// Act
		response1 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{getAccount1.AccountNumber}/transfer", new TransferRequest()
		{
			DestinationAccountNumber = getAccount2.AccountNumber,
			Amount = 200000
		});

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

		// Act
		response2 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{getAccount1.AccountNumber}/transfer", new TransferRequest()
		{
			DestinationAccountNumber = getAccount2.AccountNumber,
			Amount = 150000
		});

		// Assert
		Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

		// Act
		response1 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{getAccount1.AccountNumber}");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

		// Act
		getAccount1 = await response1.Content.ReadFromJsonAsync<Account>();

		// Assert
		Assert.NotNull(getAccount1);
		Assert.Equal(account1.Id, getAccount1.Id);
		Assert.Equal(0, getAccount1.Balance);

		// Act
		response2 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{getAccount2.AccountNumber}");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

		// Act
		getAccount2 = await response2.Content.ReadFromJsonAsync<Account>();

		// Assert
		Assert.NotNull(getAccount2);
		Assert.Equal(account2.Id, getAccount2.Id);
		Assert.Equal(300000, getAccount2.Balance);
	}
}
