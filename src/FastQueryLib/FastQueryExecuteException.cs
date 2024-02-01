using System.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace FastQueryLib
{
    /// <summary>
    /// Use InfoMessages to get Info Messages
    /// </summary>
    public class FastQueryExecuteException : Exception
    {
        public FastQueryExecuteException(string message, Exception innerException = null) : base(message, innerException)
        { }
        public List<SqlInfoMessageEventArgs> InfoMessages { get; set; }
    }
}
