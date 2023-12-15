using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FastQueryLib
{
    public static class FastQueryExtentions
    {

        #region ReadOnly

        public static async Task<FastQueryResult<bool>> IsReadOnlyAsync(this FastQuery fastQuery)
        {
            var dbName = fastQuery.Database;
            var query = $"SELECT TOP 1 is_read_only FROM sys.databases WHERE name = '{dbName}';";
            var is_read_only = await fastQuery.Clear().WithQuery(query).ExecuteScalarAsync<bool?>();
            var value = is_read_only.Result ?? throw new Exception();
            return fastQuery.Result(value);
        }

        public static async Task<FastQueryResult<int>> SetDatabaseReadOnly(this FastQuery fastQuery, bool isReadOnly)
        {
            var dbName = fastQuery.Database;
            var state = isReadOnly ? "READ_ONLY" : "READ_WRITE";
            var querySetReadOnLy = $"USE master; ALTER DATABASE {dbName} SET {state}; USE [{dbName}];";
            return await fastQuery.Clear().WithQuery(querySetReadOnLy).ExecuteNonQueryAsync();
        }

        #endregion

        #region CountNumberConnecttionOnDatabase

        public static async Task<FastQueryResult<int?>> CountNumberConnecttionOnDatabase(this FastQuery fastQuery)
        {
            var dbName = fastQuery.Database;
            var queryCheckDbReady = $"SELECT COUNT(1) FROM sys.dm_exec_sessions WHERE database_id = DB_ID('{dbName}')";
            return await fastQuery.Clear().WithQuery(queryCheckDbReady).ExecuteScalarAsync<int?>();
        }

        #endregion

        #region SingleUser

        public static async Task<FastQueryResult<bool>> IsDatabaseSingleUserAsync(this FastQuery fastQuery)
        {
            var dbName = fastQuery.Database;
            var query = $"SELECT TOP 1 user_access_desc FROM sys.databases WHERE name = '{dbName}';";
            var user_access_desc = await fastQuery.Clear().WithQuery(query).ExecuteScalarAsync<string?>();
            if (user_access_desc.Result == null)
            {
                user_access_desc.Dispose();
                throw new Exception($"Not exist database [{dbName}]");
            }

            var is_multi_user = user_access_desc.Result.Equals("MULTI_USER", StringComparison.CurrentCultureIgnoreCase) == false;
            return fastQuery.Result(is_multi_user);
        }

        public static async Task<FastQueryResult<bool>> SetDatabaseSingleUserAsync(this FastQuery fastQuery, bool isSingleUser)
        {
            var dbName = fastQuery.Database;
            var state = isSingleUser ? "SINGLE_USER WITH ROLLBACK IMMEDIATE" : "MULTI_USER";
            var query = $"USE master; ALTER DATABASE [{dbName}] SET {state}; USE [{dbName}];";
            await fastQuery.Clear().WithQuery(query).ExecuteNonQueryAsync();
            return fastQuery.Result(true);
        }

        #endregion


        #region GetStateDatabase


        public static async Task<string?> GetStateDatabase(this FastQuery fastQuery, string databasename)
        {
            var query_checking = $"USE master; SELECT state_desc FROM sys.databases WHERE name = '{databasename}';";
            using var checkingresult = await fastQuery
                .UseDatabase("master")
                .WithQuery(query_checking)
                .ExecuteScalarAsync<string?>();
            return checkingresult.Result;
        }

        public static async Task<bool> CheckDatabaseOnline(this FastQuery fastQuery, string? databasename = null)
        {
            var state = await fastQuery.GetStateDatabase(databasename ?? fastQuery.Database);
            return state?.Equals("ONLINE", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static async Task<bool> CheckDatabaseExistsAsync(this FastQuery fastQuery, string? databasename = null)
        {
            var state = await fastQuery.GetStateDatabase(databasename ?? fastQuery.Database);
            return string.IsNullOrWhiteSpace(state) == false;
        }

        #endregion

    }
}
