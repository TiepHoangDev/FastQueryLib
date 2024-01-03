using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastQueryLib
{
    public static class FastQueryExtention
    {
        public static FastQuery CreateFastQuery(this SqlConnection dbConnection)
        {
            return new FastQuery(dbConnection);
        }


        public static List<string> SYSTEM_DB = new List<string>() { "master", "model", "msdb", "tempdb" };

        public static FastQuery ThrowIfIsSystemDb(this FastQuery fastQuery, Exception exception = null)
        {
            if (SYSTEM_DB.Any(q => q.Equals(fastQuery.Database, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw (exception ?? new Exception($"Database [{fastQuery.Database}] is system database."));
            }
            return fastQuery;
        }

    }
}
