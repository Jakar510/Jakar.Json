// Jakar.Json.Dapper.Tests
// 10/17/2026

using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;



namespace Jakar.Json.Dapper.Tests;


public sealed class CustomerRow
{
    public long         Id     { get; set; }
    public string       Name   { get; set; } = "";
    public Order?       Latest { get; set; }
    public List<Order>? Orders { get; set; }
}



// Dapper's type handlers are process-wide: these tests share one registration.
[TestFixture, NonParallelizable]
public sealed class DapperTests
{
    private SqliteConnection __connection = null!;


    [OneTimeSetUp] public void Register()
    {
        JakarJsonDapper.AddTypeHandler<Order>();
        JakarJsonDapper.AddTypeHandler<List<Order>>();
        JakarJsonDapper.AddTypeHandler<Point>();
    }

    [SetUp] public void Open()
    {
        __connection = new SqliteConnection("Data Source=:memory:");
        __connection.Open();
        __connection.Execute("CREATE TABLE Customers (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Latest TEXT NULL, Orders TEXT NULL, Home BLOB NULL)");
    }

    [TearDown] public void Close() => __connection.Dispose();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Handler_SetsTextParameter_AndParsesTextOrBytes()
    {
        JakarJsonTypeHandler<Order> handler = new();
        using SqliteCommand         command = __connection.CreateCommand();
        IDbDataParameter            value   = command.CreateParameter();
        IDbDataParameter            empty   = command.CreateParameter();
        handler.SetValue(value, Samples.Order());
        handler.SetValue(empty, null);

        Assert.Multiple(() =>
                        {
                            Assert.That(value.DbType,                                                   Is.EqualTo(DbType.String));
                            Assert.That((string)value.Value!,                                           Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(empty.Value,                                                    Is.EqualTo(DBNull.Value));
                            Assert.That(handler.Parse((string)value.Value!),                            Is.EqualTo(Samples.Order()));
                            Assert.That(handler.Parse(Encoding.UTF8.GetBytes((string)value.Value!)),    Is.EqualTo(Samples.Order()), "a UTF-8 bytes column");
                        });
    }

    [Test] public void Handler_ForUnsupportedType_Throws() => Assert.Throws<NotSupportedException>(() => _ = new JakarJsonTypeHandler<PlainDto>());


    // ─── Integration: SQLite ─────────────────────────────────────────────────

    [Test] public void InsertAndQuery_RoundTrips()
    {
        __connection.Execute("INSERT INTO Customers (Name, Latest, Orders) VALUES (@Name, @Latest, @Orders)", new { Name = "Zoë", Latest = Samples.Order(), Orders = Samples.Orders() });

        CustomerRow row = __connection.QuerySingle<CustomerRow>("SELECT Id, Name, Latest, Orders FROM Customers");

        Assert.Multiple(() =>
                        {
                            Assert.That(row.Latest, Is.EqualTo(Samples.Order()));
                            Assert.That(row.Orders, Is.EqualTo(Samples.Orders()), "a List<Order> parameter is one JSON value, not expanded into a list");
                        });
    }

    [Test] public void StoredText_IsJakarJson()
    {
        __connection.Execute("INSERT INTO Customers (Name, Latest) VALUES ('a', @Latest)", new { Latest = Samples.Order() });
        Assert.That(__connection.ExecuteScalar<string>("SELECT Latest FROM Customers"), Does.Contain("\"customer\" : \"Zoë\""));
    }

    [Test] public void SingleColumn_AndNulls()
    {
        __connection.Execute("INSERT INTO Customers (Name, Latest, Orders) VALUES ('a', @Latest, NULL)", new { Latest = Samples.Order() });

        Assert.Multiple(() =>
                        {
                            Assert.That(__connection.QuerySingle<Order>("SELECT Latest FROM Customers"),           Is.EqualTo(Samples.Order()));
                            Assert.That(__connection.QuerySingle<CustomerRow>("SELECT * FROM Customers").Orders,   Is.Null);
                        });
    }

    [Test] public void StructModels_AndBytesColumns()
    {
        __connection.Execute("INSERT INTO Customers (Name, Home) VALUES ('a', CAST(@Home AS BLOB))", new { Home = new Point { X = 5, Y = 6 } });
        Assert.That(__connection.QuerySingle<Point>("SELECT Home FROM Customers"), Is.EqualTo(new Point { X = 5, Y = 6 }));
    }

    [Test] public void AddTypeHandlers_RegistersAnAssemblysModels()
    {
        int added = JakarJsonDapper.AddTypeHandlers(typeof(Order).Assembly);

        Assert.Multiple(() =>
                        {
                            Assert.That(added,                                  Is.GreaterThanOrEqualTo(3), "Order, Line, Point");
                            Assert.That(SqlMapper.HasTypeHandler(typeof(Line)), Is.True);
                            Assert.That(SqlMapper.HasTypeHandler(typeof(PlainDto)), Is.False);
                        });
    }
}
