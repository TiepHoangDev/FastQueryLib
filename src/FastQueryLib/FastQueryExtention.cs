using Microsoft.Data.SqlClient;

namespace FastQueryLib
{
    public static class FastQueryExtention
    {
        public static FastQuery CreateFastQuery(this SqlConnection dbConnection)
        {
            return new FastQuery(dbConnection);
        }

    }
}
