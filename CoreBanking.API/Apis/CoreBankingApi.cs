namespace CoreBanking.API.Apis;

public static class CoreBankingApi
{
	public static IEndpointRouteBuilder MapCoreBankingApi(this IEndpointRouteBuilder endpoint)
	{
		var vApi = endpoint.NewVersionedApi("CoreBanking");
		var v1 = vApi.MapGroup("api/v{version:apiVersion}/corebanking").HasApiVersion(1, 0);

		v1.MapGet("/customers", GetCustomers);
		v1.MapPost("/customers", CreateCustomer);

		v1.MapGet("/accounts", GetAccounts);
		v1.MapPost("/accounts", CreateAccounts);
		v1.MapPut("/accounts/{id:guid}/deposit", Deposit);
		v1.MapPut("/accounts/{id:guid}/withdraw", Withdraw);
		v1.MapPut("/accounts/{id:guid}/transfer", Transfer);

		return endpoint;
	}

	#region Transfer
	private static async Task<Results<Ok<Account>, BadRequest>> Transfer(
			[AsParameters] CoreBankingServices services,
			Guid id,
			TransferRequest transfer
		)
	{
		if (id == Guid.Empty)
		{
			services.Logger.LogError($"Account Id cannot be empty");
			return TypedResults.BadRequest();
		}

		if (string.IsNullOrEmpty(transfer.DestinationAccountNumber))
		{
			services.Logger.LogError($"Destination account number cannot be empty");
			return TypedResults.BadRequest();
		}

		if (transfer.Amount <= 0)
		{
			services.Logger.LogError($"Amount must be greater than zero");
			return TypedResults.BadRequest();
		}

		var account = await services.DbContext.Accounts.FindAsync(id);

		if (account == null)
		{
			services.Logger.LogError($"Account not found");
			return TypedResults.BadRequest();
		}

		if (account.Balance < transfer.Amount)
		{
			services.Logger.LogError($"Insufficient funds");
			return TypedResults.BadRequest();
		}

		var destinationAccount = await services.DbContext.Accounts
			.FirstOrDefaultAsync(a => a.AccountNumber == transfer.DestinationAccountNumber);

		if (destinationAccount == null)
		{
			services.Logger.LogError($"Destination account not found");
			return TypedResults.BadRequest();
		}

		account.Balance -= transfer.Amount;
		destinationAccount.Balance += transfer.Amount;

		try
		{
			var now = DateTime.UtcNow;

			// Add a withdrawal transaction for the source account
			services.DbContext.Transactions.Add(new Transaction
			{
				Id = Guid.CreateVersion7(),
				AccountId = account.Id,
				Amount = transfer.Amount,
				DateUtc = now,
				Type = TransactionTypes.Withdraw
			});

			// Add a deposit transaction for the destination account
			services.DbContext.Transactions.Add(new Transaction
			{
				Id = Guid.CreateVersion7(),
				AccountId = destinationAccount.Id,
				Amount = transfer.Amount,
				DateUtc = now,
				Type = TransactionTypes.Deposit
			});

			await services.DbContext.SaveChangesAsync();
			return TypedResults.Ok(account);
		}
		catch (Exception ex)
		{
			services.Logger.LogError(ex, "An error occured while transfering");
			return TypedResults.BadRequest();
		}
	}
	#endregion

	#region Withdraw
	private static async Task<Results<Ok<Account>, BadRequest>> Withdraw(
			[AsParameters] CoreBankingServices services,
			Guid id,
			WithdrawalRequest withdrawal
		)
	{
		if (id == Guid.Empty)
		{
			services.Logger.LogError($"Account Id cannot be empty");
			return TypedResults.BadRequest();
		}

		if (withdrawal.Amount <= 0)
		{
			services.Logger.LogError($"Amount must be greater than zero");
			return TypedResults.BadRequest();
		}

		var account = await services.DbContext.Accounts.FindAsync(id);

		if (account == null)
		{
			services.Logger.LogError($"Account not found");
			return TypedResults.BadRequest();
		}

		account.Balance -= withdrawal.Amount;
		if (account.Balance < 0)
		{
			services.Logger.LogError($"Insufficient funds");
			return TypedResults.BadRequest();
		}

		try
		{
			services.DbContext.Transactions.Add(new Transaction
			{
				Id = Guid.CreateVersion7(),
				AccountId = account.Id,
				Amount = withdrawal.Amount,
				DateUtc = DateTime.UtcNow,
				Type = TransactionTypes.Withdraw
			});
			services.DbContext.Update(account);
			await services.DbContext.SaveChangesAsync();

			services.Logger.LogInformation("Withdrawn successfully");
			return TypedResults.Ok(account);
		}
		catch (Exception ex)
		{
			services.Logger.LogError(ex, "An error occured while withdrawing");
			return TypedResults.BadRequest();
		}
	}
	#endregion

	#region Deposit
	public static async Task<Results<Ok<Account>, BadRequest>> Deposit(
			[AsParameters] CoreBankingServices services,
			Guid id,
			DepositionRequest deposition
		)
	{
		if (id == Guid.Empty)
		{
			services.Logger.LogError($"Account Id cannot be empty");
			return TypedResults.BadRequest();
		}

		if (deposition.Amount <= 0)
		{
			services.Logger.LogError($"Amount must be greater than zero");
			return TypedResults.BadRequest();
		}

		var account = await services.DbContext.Accounts.FindAsync(id);

		if (account == null)
		{
			services.Logger.LogError($"Account not found");
			return TypedResults.BadRequest();
		}

		account.Balance += deposition.Amount;
		try
		{
			services.DbContext.Transactions.Add(new Transaction
			{
				Id = Guid.CreateVersion7(),
				AccountId = id,
				Amount = deposition.Amount,
				DateUtc = DateTime.UtcNow,
				Type = TransactionTypes.Deposit
			});
			services.DbContext.Update(account);
			await services.DbContext.SaveChangesAsync();

			services.Logger.LogInformation("Deposited successfully");
			return TypedResults.Ok(account);
		}
		catch (Exception ex)
		{
			services.Logger.LogError(ex, "An error occured while depositing");
			return TypedResults.BadRequest();
		}
	}
	#endregion

	#region Account
	public static async Task<Results<Ok<Account>, BadRequest>> CreateAccounts(
			[AsParameters] CoreBankingServices services,
			Account account
		)
	{
		if (account.CustomerId == Guid.Empty)
		{
			services.Logger.LogError($"Customer Id cannot be empty");
			return TypedResults.BadRequest();
		}

		account.Id = Guid.CreateVersion7();
		account.Balance = 0;
		account.AccountNumber = GenerateAccountNumber();

		services.DbContext.Accounts.Add(account);
		await services.DbContext.SaveChangesAsync();

		services.Logger.LogInformation($"Account created: {account.CustomerId} - {account.AccountNumber}");

		return TypedResults.Ok(account);
	}

	private static string GenerateAccountNumber()
	{
		return DateTime.UtcNow.Ticks.ToString();
	}

	private static async Task<Ok<PaginationResponse<Account>>> GetAccounts(
			[AsParameters] CoreBankingServices services,
			[AsParameters] PaginationRequest pagination,
			Guid? customerId = null
		)
	{
		services.Logger.LogInformation($"Fetching accounts with pagination: PageSize={pagination.PageSize}, PageIndex={pagination.PageIndex}");

		IQueryable<Account> accounts = services.DbContext.Accounts;

		if (customerId.HasValue)
		{
			accounts = accounts.Where(a => a.CustomerId == customerId);
		}

		return TypedResults.Ok(new PaginationResponse<Account>(
			pagination.PageIndex,
			pagination.PageSize,
			await accounts.CountAsync(),
			await accounts
				.OrderBy(c => c.AccountNumber)
				.Skip(pagination.PageIndex * pagination.PageSize)
				.Take(pagination.PageSize)
				.ToListAsync()
		));
	}
	#endregion

	#region Customer
	public static async Task<Results<Ok<Customer>, BadRequest>> CreateCustomer(
			[AsParameters] CoreBankingServices services,
			Customer customer
		)
	{
		if (string.IsNullOrEmpty(customer.Name))
		{
			services.Logger.LogError($"Customer name cannot be empty");
			return TypedResults.BadRequest();
		}

		customer.Address ??= "";

		if (customer.Id == Guid.Empty)
		{
			customer.Id = Guid.CreateVersion7();
		}

		services.DbContext.Customers.Add(customer);
		await services.DbContext.SaveChangesAsync();

		services.Logger.LogInformation($"Customer created: {customer.Id} - {customer.Name}");

		return TypedResults.Ok(customer);
	}

	private static async Task<Ok<PaginationResponse<Customer>>> GetCustomers(
			[AsParameters] CoreBankingServices services,
			[AsParameters] PaginationRequest pagination
		)
	{
		services.Logger.LogInformation($"Fetching customers with pagination: PageSize={pagination.PageSize}, PageIndex={pagination.PageIndex}");

		return TypedResults.Ok(new PaginationResponse<Customer>(
			pagination.PageIndex,
			pagination.PageSize,
			await services.DbContext.Customers.CountAsync(),
			await services.DbContext.Customers
				.OrderBy(c => c.Name)
				.Skip(pagination.PageIndex * pagination.PageSize)
				.Take(pagination.PageSize)
				.ToListAsync()
		));
	}
	#endregion
}