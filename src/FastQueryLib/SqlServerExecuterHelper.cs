using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

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

        public static SqlConnectionStringBuilder CreateConnectionString(string server, string database = "master", string username = null, string pass = null)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server ?? throw new ArgumentNullException(nameof(server)),
                InitialCatalog = database ?? throw new ArgumentNullException(nameof(database)),
                Encrypt = false,
                MultipleActiveResultSets = true
            };

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

        public static SqlConnection CreateConnection(string sqlConnectionString)
        {
            return new SqlConnection(sqlConnectionString);
        }

        public static SqlCommand CreateCommand(this SqlConnection dbConnection, string commandText, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text, SqlTransaction transaction = null, int commandTimeoutSecond = 30)
        {
            var command = new SqlCommand(commandText, dbConnection, transaction)
            {
                CommandTimeout = commandTimeoutSecond,
                CommandType = commandType
            };
            command.AddParameters(parameters);
            return command;
        }

        public static bool AddParameters<T>(this T command, Dictionary<string, object> parameters) where T : IDbCommand
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

        public static T ReadFirstAs<T>(this SqlDataReader reader) where T : class, new()
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
                    var value = reader[prop.Key];
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
    }
}
