using Dapper;
using HelpdeskApi.Models;

namespace HelpdeskApi.Data;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<int> CreateAsync(CreateCustomerRequest request);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _factory;

    public CustomerRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<Customer>(
            "SELECT CustomerId, Name, Email, Company, CreatedAt FROM Customers ORDER BY Name");
    }

    public async Task<int> CreateAsync(CreateCustomerRequest request)
    {
        using var db = _factory.Create();
        return await db.QuerySingleAsync<int>(@"
            INSERT INTO Customers (Name, Email, Company)
            OUTPUT INSERTED.CustomerId
            VALUES (@Name, @Email, @Company)", request);
    }
}
