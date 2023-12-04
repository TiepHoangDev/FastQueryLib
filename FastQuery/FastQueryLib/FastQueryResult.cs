namespace FastQueryLib
{
    public record FastQueryResult<T>(FastQuery FastQuery, T Result) : IDisposable
    {
        public void Dispose()
        {
            FastQuery?.Dispose();
        }

        public static implicit operator T(FastQueryResult<T> fastQuery) => fastQuery.Result;
    }
}
