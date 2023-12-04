using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace FastQueryLib
{
    public static class SqlServerExecuterHelper
    {
        public static SqlConnection CreateConnection(this SqlConnectionStringBuilder sqlConnectionString)
        {
            var cs = sqlConnectionString.ConnectionString;
            return new SqlConnection(cs);
        }

        public static SqlConnection CreateOpenConnection(this SqlConnectionStringBuilder sqlConnectionString)
        {
            var con = CreateConnection(sqlConnectionString);
            con.Open();
            return con;
        }

        public static SqlConnection NewOpenConnectToDatabase(this SqlConnection sqlConnection, string newDatabasename)
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
            var sb = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
            sb.InitialCatalog = newDatabasename;
            return sb.CreateOpenConnection();
        }

        public static SqlConnectionStringBuilder CreateConnectionString(string server, string database = "master", string? username = null, string? pass = null)
        {
            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = server ?? throw new ArgumentNullException(nameof(server));
            builder.InitialCatalog = database ?? throw new ArgumentNullException(nameof(database));
            builder.Encrypt = false;
            builder.MultipleActiveResultSets = true;

            if (string.IsNullOrWhiteSpace(username))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = username ?? throw new ArgumentNullException(nameof(username));
                builder.Password = pass ?? throw new ArgumentNullException(nameof(pass));
            }

            return builder;
        }

        public static SqlCommand CreateCommand(this SqlConnection dbConnection, string commandText, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null, int commandTimeoutSecond = 30)
        {
            var command = new SqlCommand(commandText, dbConnection, transaction);
            command.CommandTimeout = commandTimeoutSecond;
            command.CommandType = commandType;
            command.AddParameters(parameters);
            return command;
        }

        public static bool AddParameters<T>(this T command, Dictionary<string, object>? parameters) where T : IDbCommand
        {
            if (parameters?.Any() == true)
            {
                foreach (var item in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = item.Key.StartsWith("@") ? item.Key : ("@" + item.Key);
                    param.Value = item.Value;
                    command.Parameters.Add(param);
                }
                return true;
            }
            return false;
        }

        public static bool ExecuteSuccess(this SqlCommand command)
        {
            return command.ExecuteNonQuery() > 0;
        }

        public static T? ReadFirstAs<T>(this SqlDataReader reader) where T : class, new()
        {
            return ReadAs<T>(reader).FirstOrDefault();
        }

        public static IEnumerable<T> ReadAs<T>(this SqlDataReader reader) where T : class, new()
        {
            var props = reader.GetColumnMapProperty<T>();
            while (reader.Read())
            {
                var instance = Activator.CreateInstance<T>();
                foreach (var prop in props)
                {
                    var value = reader.GetValue(prop.Key);
                    if (value is DBNull == false)
                    {
                        prop.Value.SetValue(instance, value);
                    }
                }
                yield return instance;
            }
        }

        public static Dictionary<string, PropertyInfo> GetColumnMapProperty<T>(this SqlDataReader reader) where T : class, new()
        {
            var type = typeof(T);
            var propsQuery = from x in reader.GetColumnSchema()
                             let prop = type.GetProperty(x.ColumnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                             where prop != null
                             select new { x.ColumnName, prop };
            var props = propsQuery.ToDictionary(q => q.ColumnName, q => q.prop);
            return props;
        }

        #region GetStateDatabase


        public static async Task<string?> GetStateDatabase(this SqlConnection dbConnection, string databasename)
        {
            var query_checking = $"SELECT name, state_desc FROM sys.databases WHERE name = '{databasename}'";
            using var checkingresult = await dbConnection
                .NewOpenConnectToDatabase("master")
                .CreateFastQuery()
                .WithQuery(query_checking)
                .ExecuteReadAsyncAs<InfoDatabase>();
            return checkingresult.Result.FirstOrDefault()?.state_desc;
        }

        public static async Task<bool> CheckDatabaseOnline(this SqlConnection dbConnection, string? databasename = null)
        {
            var state = await GetStateDatabase(dbConnection, databasename ?? dbConnection.Database);
            return state?.Equals("ONLINE", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static async Task<bool> CheckDatabaseExistsAsync(this SqlConnection dbConnection, string? databasename = null)
        {
            var state = await GetStateDatabase(dbConnection, databasename ?? dbConnection.Database);
            return string.IsNullOrWhiteSpace(state) == false;
        }

        record InfoDatabase
        {
            public string? name { get; set; }
            public string? state_desc { get; set; }
        }

        #endregion

    }
}
