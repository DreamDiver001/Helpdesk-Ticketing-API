using System.Data;
using Microsoft.Data.SqlClient;

namespace HelpdeskApi.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HelpdeskDb")
            ?? throw new InvalidOperationException("Connection string 'HelpdeskDb' is missing.");
    }

    public IDbConnection Create() => new SqlConnection(_connectionString);
}
