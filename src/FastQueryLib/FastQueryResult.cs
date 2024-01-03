using System;

namespace FastQueryLib
{
    public class FastQueryResult<T> : IDisposable
    {
        public FastQuery FastQuery { get; private set; }
        public T Result { get; private set; }

        public FastQueryResult(FastQuery fastQuery, T result)
        {
            FastQuery = fastQuery;
            Result = result;
        }

        public void Dispose()
        {
            FastQuery?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static implicit operator T(FastQueryResult<T> fastQuery) => fastQuery.Result;
    }
}
