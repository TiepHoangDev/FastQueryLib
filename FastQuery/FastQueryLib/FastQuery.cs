using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace FastQueryLib
{
    public class FastQuery : IDisposable
    {
        private readonly SqlCommand _sqlCommand;
        private readonly List<SqlInfoMessageEventArgs> _infoMessages;

        public FastQuery(SqlConnection sqlConnection)
        {
            _sqlCommand = new SqlCommand()
            {
                Connection = sqlConnection
            };
            _infoMessages = new List<SqlInfoMessageEventArgs>();
            _sqlCommand.Connection.InfoMessage += Connection_InfoMessage;
        }

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Debug.WriteLine(e);
            _infoMessages.Add(e);
        }

        public void Dispose()
        {
            _sqlCommand.Connection.InfoMessage -= Connection_InfoMessage;
            _sqlCommand.Dispose();
            if (_sqlCommand.Connection != null)
            {
                if (_sqlCommand.Connection.State != ConnectionState.Closed) _sqlCommand.Connection.Close();
            }
            GC.SuppressFinalize(this);
        }

        public FastQuery WithQuery(string commandText) => WithCustom(q => q.CommandText = commandText);
        public FastQuery WithParameters(Dictionary<string, object> parameters) => WithCustom(q => q.AddParameters(parameters));
        public FastQuery WithTransaction(SqlTransaction? transaction = null)
        {
            if (transaction == null)
            {
                EnsureOpenConnection();
                transaction = _sqlCommand.Connection.BeginTransaction();
            }
            return WithCustom(q => q.Transaction = transaction);
        }
        public FastQuery WithCommandType(CommandType commandType) => WithCustom(q => q.CommandType = commandType);
        public FastQuery WithTimeout(int commandTimeoutSecond) => WithCustom(q => q.CommandTimeout = commandTimeoutSecond);

        public FastQuery WithCustom(Action<SqlCommand> custom)
        {
            if (_sqlCommand == null) throw new Exception($"Call method {nameof(WithQuery)} to init {nameof(_sqlCommand)}");
            custom?.Invoke(_sqlCommand);
            return this;
        }

        public async Task<T> ExecuteAsync<T>(Func<SqlCommand, Task<T>> execute)
        {
            try
            {
                EnsureOpenConnection();

                Debug.WriteLine($"ExecuteAsync: {_sqlCommand.CommandText} with Transaction");
                var result = await execute.Invoke(_sqlCommand);

                if (_sqlCommand.Transaction != null)
                {
                    await _sqlCommand.Transaction.CommitAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (_sqlCommand.Transaction != null)
                {
                    await _sqlCommand.Transaction.RollbackAsync();
                }
                throw new Exception(ex.Message, new Exception(JsonSerializer.Serialize(_infoMessages)));
            }
        }

        public void EnsureOpenConnection()
        {
            if (_sqlCommand.Connection.State != ConnectionState.Open)
            {
                Debug.WriteLine($"Open connection: {_sqlCommand.Connection.Database}");
                _sqlCommand.Connection.Open();
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

        public async Task<FastQueryResult<int>> ExecuteNumberOfRowsAsync() => new FastQueryResult<int>(this, await ExecuteAsync(q => q.ExecuteNonQueryAsync()));

    }
}
