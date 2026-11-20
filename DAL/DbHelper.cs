using Microsoft.Data.SqlClient;

namespace UshiMania.DAL;

public sealed class DbHelper
{
    private readonly string connectionString;

    public DbHelper(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("UshiManiaConn")
            ?? throw new InvalidOperationException("Connection string 'UshiManiaConn' was not found.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }

    public SqlCommand CreateCommand(
        SqlConnection connection,
        string commandText,
        SqlTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

        if (transaction is not null && !ReferenceEquals(transaction.Connection, connection))
        {
            throw new ArgumentException("Giao dịch (transaction) phải thuộc về kết nối được cung cấp.", nameof(transaction));
        }

        return new SqlCommand(commandText, connection, transaction);
    }
}
