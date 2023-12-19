using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace FastQueryLib
{
    public class FastQuery : IDisposable
    {

        #region VARIABLE

        public readonly List<SqlInfoMessageEventArgs> InfoMessages = new();

        protected SqlCommand SqlCommand;

        #endregion

        #region FIELDs
        public int Id => SqlCommand.Connection.ClientConnectionId.GetHashCode();
        public string Database => SqlCommand.Connection.Database;
        public bool Disposed { get; set; }
        #endregion

        public FastQuery(SqlConnection sqlConnection)
        {
            SqlCommand = new SqlCommand()
            {
                Connection = sqlConnection
            };
            SqlCommand.Connection.InfoMessage += Connection_InfoMessage;
        }

        #region LOG

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            WriteLines(e);
            InfoMessages.Add(e);
        }

        private void WriteLines(Action writeContent)
        {
            Debug.WriteLine($"=============={this}==============");
            writeContent.Invoke();
            Debug.WriteLine($"==================================");
        }

        private void WriteLines(object msg) => WriteLines(() => Debug.WriteLine(msg));

        public override string ToString()
        {
            return $"{nameof(FastQuery)} [Id={Id}]";
        }

        #endregion

        #region SETTING METHODS

        public FastQuery WithQuery(string commandText) => WithCustom(q => q.CommandText = commandText);
        public FastQuery WithParameters(Dictionary<string, object> parameters) => WithCustom(q => q.AddParameters(parameters));
        public FastQuery WithTransaction(SqlTransaction? transaction = null)
        {
            if (transaction == null)
            {
                EnsureOpenConnection();
                transaction = SqlCommand.Connection.BeginTransaction();
            }
            return WithCustom(q => q.Transaction = transaction);
        }
        public FastQuery WithCommandType(CommandType commandType) => WithCustom(q => q.CommandType = commandType);
        public FastQuery WithTimeout(int commandTimeoutSecond) => WithCustom(q => q.CommandTimeout = commandTimeoutSecond);
        public FastQuery WithCustom(Action<SqlCommand> custom)
        {
            custom.Invoke(SqlCommand);
            return this;
        }

        #endregion

        #region LOGIC METHODS 

        public FastQuery UseDatabase(string dbName)
        {
            var conn = SqlCommand.Connection;
            if (!conn.Database.Equals(dbName, StringComparison.CurrentCultureIgnoreCase))
            {
                if (conn.State == ConnectionState.Open)
                {
                    SqlCommand.Connection.ChangeDatabase(dbName);
                }
                else
                {
                    var builder = new SqlConnectionStringBuilder(conn.ConnectionString)
                    {
                        InitialCatalog = dbName
                    };
                    SqlCommand.Connection = new SqlConnection(builder.ConnectionString);
                }
            }
            return this;
        }

        public FastQuery Clear()
        {
            //Rollback Transaction
            SqlCommand.Transaction?.Rollback();
            SqlCommand.Transaction = null;

            //create new
            var Connection = SqlCommand.Connection;
            SqlCommand = new SqlCommand()
            {
                Connection = Connection
            };
            return this;
        }


        #endregion

        #region Dispose

        public void Dispose()
        {
            SqlCommand.Connection.InfoMessage -= Connection_InfoMessage;
            SqlCommand.Connection.Close();
            SqlCommand.Connection.Dispose();
            SqlCommand.Dispose();
            Debug.WriteLine($"{this} Dispose");
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Open/Close

        public bool CloseConnection()
        {
            SqlCommand?.Connection?.Close();
            var state = SqlCommand?.Connection?.State ?? ConnectionState.Closed;
            return state == ConnectionState.Closed;
        }

        public void EnsureOpenConnection()
        {
            if (SqlCommand.Connection.State != ConnectionState.Open)
            {
                SqlCommand.Connection.Open();
            }
        }

        #endregion

        public FastQuery ThrowIfDisposed(Exception? exception = null)
        {
            if (Disposed)
            {
                throw exception ?? throw new Exception($"{nameof(FastQuery)} has disposed. {this}");
            }
            return this;
        }

        #region EXECUTE

        public async Task<T> ExecuteAsync<T>(Func<SqlCommand, Task<T>> execute)
        {
            ThrowIfDisposed();

            try
            {
                EnsureOpenConnection();
                WriteLines(() =>
                {
                    Debug.WriteLine(SqlCommand.CommandText);
                    Debug.WriteLineIf(SqlCommand.Transaction != null, $"---------------\n{this} with Transaction", "ExecuteAsync");
                });

                var result = await execute.Invoke(SqlCommand);

                if (SqlCommand.Transaction != null)
                {
                    await SqlCommand.Transaction.CommitAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                WriteLines(ex);
                if (SqlCommand.Transaction != null)
                {
                    await SqlCommand.Transaction.RollbackAsync();
                }
                var json = InfoMessages.Any() ? new Exception(JsonSerializer.Serialize(InfoMessages)) : null;
                throw new Exception($"{SqlCommand?.CommandText} => {ex.Message}", json);
            }
        }

        public async Task<FastQueryResult<List<T>>> ExecuteReadAsyncAs<T>() where T : class, new()
        {
            var data = await ExecuteAsync(async q =>
            {
                using var reader = await q.ExecuteReaderAsync();
                return reader.ReadAs<T>().ToList();
            });
            return new FastQueryResult<List<T>>(this, data);
        }

        public async Task<FastQueryResult<int>> ExecuteNonQueryAsync()
            => new FastQueryResult<int>(this, await ExecuteAsync(q => q.ExecuteNonQueryAsync()));
        public async Task<FastQueryResult<T?>> ExecuteScalarAsync<T>()
        {
            object? value = await ExecuteAsync(async q => await q.ExecuteScalarAsync());
            return new FastQueryResult<T?>(this, (T?)value);
        }

        #endregion

        #region Create Result

        public FastQueryResult<T> Result<T>(T value)
        {
            return new FastQueryResult<T>(this, value);
        }

        #endregion

    }
}
