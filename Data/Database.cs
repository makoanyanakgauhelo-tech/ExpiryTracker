using System.Configuration;
using System.Data.SqlClient;

namespace ExpiryTracker.Data
{
    public static class Database
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["ExpiryDb"].ConnectionString;

        public static SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }
}
