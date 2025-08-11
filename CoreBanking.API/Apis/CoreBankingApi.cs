using Microsoft.AspNetCore.Mvc.Diagnostics;

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

	private static async Task Transfer(Guid id)
	{
		throw new NotImplementedException();
	}

	private static async Task Withdraw(Guid id)
	{
		throw new NotImplementedException();
	}

	private static async Task Deposit(Guid id)
	{
		throw new NotImplementedException();
	}

	private static async Task CreateAccounts(HttpContext context)
	{
		throw new NotImplementedException();
	}

	private static async Task GetAccounts([AsParameters] PaginationRequest pagination)
	{
		throw new NotImplementedException();
	}

	private static async Task<Results<Ok<Customer>, BadRequest>> CreateCustomer(
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
			//[AsParameters] PaginationRequest pagination,
			int pageSize = 10,
			int pageIndex = 0
		)
	{
		services.Logger.LogInformation($"Fetching customers with pagination: PageSize={pageSize}, PageIndex={pageIndex}");

		var pagination = new PaginationRequest(pageSize, pageIndex);
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
}
