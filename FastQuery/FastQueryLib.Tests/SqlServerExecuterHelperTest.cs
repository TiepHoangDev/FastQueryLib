using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using FastQueryLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastQueryLib.Tests
{
    public class SqlServerExecuterHelperTest : TestBase
    {
        static string DATABASE = Config.Source.Database;
        static string SQLSERVER = Config.Source.ServerName;
        static int SQLPORT = Config.Source.SqlPort;


        #region CreateConnection


        [TestCaseSource(nameof(CreateConnectionStringData))]
        public void CreateConnection(CreateConnectionString_InputTest create)
        {
            if (create.isError)
            {
                Assert.Throws<ArgumentNullException>(() => SqlServerExecuterHelper.CreateConnectionString(create.server, create.database, create.username, create.pass));
                return;
            }

            var cs = SqlServerExecuterHelper.CreateConnectionString(create.server, create.database, create.username, create.pass);
            Assert.IsNotNull(cs);
            Assert.IsNotNull(cs.ConnectionString);
            Assert.IsNotEmpty(cs.ConnectionString);
            Console.WriteLine(cs.ConnectionString);

            using var ct = SqlServerExecuterHelper.CreateConnection(cs);

            if (create.connectable)
            {
                ct.Open();
                Assert.That(ct.State, Is.EqualTo(ConnectionState.Open));
                ct.Close();
                Assert.That(ct.State, Is.EqualTo(ConnectionState.Closed));
                return;
            }

            Assert.Throws<SqlException>(() => ct.Open());
        }

        public static IEnumerable<CreateConnectionString_InputTest> CreateConnectionStringData()
        {
            return CreateConnectionString_InputTest.Samples(SQLSERVER, DATABASE, SQLPORT);
        }

        #endregion


        #region ReadAs

        [TestCaseSource(nameof(ReadAs_TestCaseSource))]
        public async Task ReadAs(ReadAs_InputTest inputTest)
        {
            using var con = SqlServerExecuterHelper.CreateConnectionString(SQLSERVER, DATABASE).CreateOpenConnection();
            using var reader = await con.CreateCommand(inputTest.query).ExecuteReaderAsync();
            var data = reader.ReadAs<Product>().ToList();
            Assert.IsNotNull(data);
            Assert.IsNotEmpty(data);
            Assert.That(data.Count, Is.EqualTo(inputTest.CountData));
            Assert.IsNull(data.FirstOrDefault()?.AdditionProp);

            Assert.IsTrue(data.All(q => q.ID != null));
            Assert.IsTrue(data.All(q => q.Name != null));
            Assert.IsTrue(data.All(q => q.CreatedTime != null));
        }

        [TestCaseSource(nameof(ReadAs_TestCaseSource))]
        public async Task FastQueryTest(ReadAs_InputTest inputTest)
        {
            using var con = SqlServerExecuterHelper.CreateConnectionString(SQLSERVER, DATABASE).CreateOpenConnection();
            using var result = await con.CreateFastQuery().WithQuery(inputTest.query).ExecuteReadAsyncAs<Product>();
            var data = result.Result;
            Assert.IsNotNull(data);
            Assert.IsNotEmpty(data);
            Assert.That(data.Count, Is.EqualTo(inputTest.CountData));
            Assert.IsNull(data.FirstOrDefault()?.AdditionProp);

            Assert.IsTrue(data.All(q => q.ID != null));
            Assert.IsTrue(data.All(q => q.Name != null));
            Assert.IsTrue(data.All(q => q.CreatedTime != null));
        }

        public static IEnumerable<ReadAs_InputTest> ReadAs_TestCaseSource() => ReadAs_InputTest.Samples();

        #endregion

    }

    public record Product
    {
        public Guid? ID { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? AdditionProp { get; set; }
    }

    public record ReadAs_InputTest(string query, int CountData)
    {
        public static IEnumerable<ReadAs_InputTest> Samples()
        {
            yield return new ReadAs_InputTest("SELECT TOP (1000) [id] ,[name] ,[createdtime] FROM [A].[dbo].[Products]", 2);
        }
    }

    public record CreateConnectionString_InputTest(string? server, string? database = "master", string? username = null, string? pass = null)
    {
        public bool isError { get; set; }
        public bool connectable { get; set; }

        public static IEnumerable<CreateConnectionString_InputTest> Samples(string SQLVERVER, string DATABASE, int SQLPORT)
        {
            yield return new CreateConnectionString_InputTest(null, null) { isError = true };
            yield return new CreateConnectionString_InputTest(null, null, null, "1") { isError = true };
            yield return new CreateConnectionString_InputTest(null, null, "1", null) { isError = true };
            yield return new CreateConnectionString_InputTest(SQLVERVER, DATABASE, "1", null) { isError = true };
            yield return new CreateConnectionString_InputTest($"127.0.0.1a,{SQLPORT}", "Aqwe", null, null) { connectable = false };
            yield return new CreateConnectionString_InputTest($"127.0.0.qwe1,{SQLPORT}", "Aqwe", null, null) { connectable = false };

            yield return new CreateConnectionString_InputTest(SQLVERVER, DATABASE, null, null) { connectable = true };
            yield return new CreateConnectionString_InputTest(SQLVERVER, DATABASE, " ", " ") { connectable = true };
            yield return new CreateConnectionString_InputTest(SQLVERVER, DATABASE, null, null) { connectable = true };
            yield return new CreateConnectionString_InputTest(SQLVERVER, DATABASE, "1", "1") { connectable = true };
            yield return new CreateConnectionString_InputTest($"127.0.0.1,{SQLPORT}", DATABASE, "1", "1") { connectable = true };
            yield return new CreateConnectionString_InputTest($"127.0.0.1,{SQLPORT}", DATABASE, null, null) { connectable = true };
        }

    }
}
