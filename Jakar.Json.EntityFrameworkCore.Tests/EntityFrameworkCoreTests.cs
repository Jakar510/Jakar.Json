// Jakar.Json.EntityFrameworkCore.Tests
// 10/17/2026

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;



namespace Jakar.Json.EntityFrameworkCore.Tests;


public sealed class Customer
{
    public int          Id     { get; set; }
    public string       Name   { get; set; } = "";
    public Order        Latest { get; set; } = new();
    public List<Order>? Orders { get; set; }
    public Point        Home   { get; set; }
}



public sealed class ShopContext( DbContextOptions<ShopContext> options, bool convention ) : DbContext(options)
{
    public DbSet<Customer> Customers  => Set<Customer>();
    public bool            Convention => convention;


    protected override void ConfigureConventions( ModelConfigurationBuilder builder )
    {
        // The convention form: every Order / List<Order> / Point property becomes a JSON column.
        if ( !convention ) { return; }

        builder.Properties<Order>().HaveJakarJsonConversion();
        builder.Properties<List<Order>>().HaveJakarJsonConversion();
        builder.Properties<Point>().HaveJakarJsonConversion();
    }

    protected override void OnModelCreating( ModelBuilder model )
    {
        if ( convention ) { return; }

        model.Entity<Customer>(static entity =>
                               {
                                   entity.Property(static x => x.Latest).HasJakarJsonConversion();
                                   entity.Property(static x => x.Orders).HasJakarJsonConversion();
                                   entity.Property(static x => x.Home).HasJakarJsonConversion();
                               });
    }
}



[TestFixture(false, TestName = "PerProperty")]
[TestFixture(true,  TestName = "Convention")]
public sealed class EntityFrameworkCoreTests( bool convention )
{
    private SqliteConnection __connection = null!;


    [SetUp] public void Open()
    {
        __connection = new SqliteConnection("Data Source=:memory:");
        __connection.Open();
        using ShopContext context = Create();
        context.Database.EnsureCreated();
    }

    [TearDown] public void Close() => __connection.Dispose();

    private ShopContext Create() => new(new DbContextOptionsBuilder<ShopContext>().UseSqlite(__connection).ReplaceService<IModelCacheKeyFactory, ModeCacheKeyFactory>().Options, convention);


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Model_MapsJsonPropertiesToText()
    {
        using ShopContext context = Create();
        IEntityType       entity  = context.Model.FindEntityType(typeof(Customer))!;

        Assert.Multiple(() =>
                        {
                            foreach ( string name in new[] { nameof(Customer.Latest), nameof(Customer.Orders), nameof(Customer.Home) } )
                            {
                                IProperty property = entity.FindProperty(name)!;
                                Assert.That(property.GetValueConverter()?.ProviderClrType, Is.EqualTo(typeof(string)), name);
                                Assert.That(property.GetValueComparer().GetType().Name,     Does.StartWith("JakarJsonValueComparer"), name);
                            }
                        });
    }

    [Test] public void Converter_And_Comparer()
    {
        JakarJsonValueConverter<Order> converter = new();
        JakarJsonValueComparer<Order>  comparer  = new();
        string                         text      = (string)converter.ConvertToProvider(Samples.Order())!;
        Order                          copy      = comparer.Snapshot(Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(text,                                                   Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(converter.ConvertFromProvider(text),                    Is.EqualTo(Samples.Order()));
                            Assert.That(comparer.Equals(Samples.Order(), Samples.Order()),      Is.True, "equal by content");
                            Assert.That(comparer.Equals(Samples.Order(), Samples.Order(8)),     Is.False);
                            Assert.That(comparer.GetHashCode(Samples.Order()),                  Is.EqualTo(comparer.GetHashCode(copy)), "equal content, equal hash");
                            Assert.That(copy,                                                   Is.EqualTo(Samples.Order()).And.Not.SameAs(Samples.Order()));
                            Assert.That(copy.Lines,                                             Is.Not.SameAs(Samples.Order().Lines), "a deep copy");
                        });
    }


    // ─── Integration: SQLite ─────────────────────────────────────────────────

    [Test] public void SaveAndLoad_RoundTrips()
    {
        using ( ShopContext context = Create() )
        {
            context.Customers.Add(new Customer { Name = "Zoë", Latest = Samples.Order(), Orders = Samples.Orders(), Home = new Point { X = 1, Y = 2 } });
            context.SaveChanges();
        }

        using ShopContext fresh    = Create();
        Customer          customer = fresh.Customers.Single();

        Assert.Multiple(() =>
                        {
                            Assert.That(customer.Latest, Is.EqualTo(Samples.Order()));
                            Assert.That(customer.Orders, Is.EqualTo(Samples.Orders()));
                            Assert.That(customer.Home,   Is.EqualTo(new Point { X = 1, Y = 2 }));
                        });
    }

    [Test] public void StoredText_IsJakarJson_AndNullsStayNull()
    {
        using ( ShopContext context = Create() )
        {
            context.Customers.Add(new Customer { Name = "a", Latest = Samples.Order() });
            context.SaveChanges();
        }

        using SqliteCommand command = __connection.CreateCommand();
        command.CommandText = "SELECT Latest, Orders FROM Customers";
        using SqliteDataReader reader = command.ExecuteReader();
        reader.Read();

        Assert.Multiple(() =>
                        {
                            Assert.That(reader.GetString(0), Does.Contain("\"customer\" : \"Zoë\""));
                            Assert.That(reader.IsDBNull(1),  Is.True, "a null collection is a NULL column");
                        });
    }

    [Test] public void ChangesInsideTrackedModels_AreSaved()
    {
        using ( ShopContext context = Create() )
        {
            context.Customers.Add(new Customer { Name = "a", Latest = Samples.Order(), Orders = Samples.Orders() });
            context.SaveChanges();
        }

        using ( ShopContext context = Create() )
        {
            Customer customer = context.Customers.Single();
            customer.Orders!.Add(Samples.Order(3));                       // mutate inside the tracked list
            customer.Latest.Lines.Add(new Line { Sku = "B-2", Quantity = 1, Price = 1m });

            Assert.That(context.ChangeTracker.DetectChanges, Throws.Nothing);
            Assert.That(context.Entry(customer).Property(static x => x.Orders).IsModified, Is.True, "the comparer sees the change");
            context.SaveChanges();
        }

        using ShopContext fresh = Create();
        Customer          saved = fresh.Customers.Single();

        Assert.Multiple(() =>
                        {
                            Assert.That(saved.Orders,       Has.Count.EqualTo(3));
                            Assert.That(saved.Latest.Lines, Has.Count.EqualTo(2));
                        });
    }

    [Test] public void UnchangedModels_AreNotUpdated()
    {
        using ( ShopContext context = Create() )
        {
            context.Customers.Add(new Customer { Name = "a", Latest = Samples.Order() });
            context.SaveChanges();
        }

        using ShopContext reread   = Create();
        Customer          customer = reread.Customers.Single();
        customer.Latest = Samples.Order(); // a different instance, same content

        reread.ChangeTracker.DetectChanges();
        Assert.That(reread.Entry(customer).State, Is.EqualTo(EntityState.Unchanged));
    }

    [Test] public void Unsupported_Throws() => Assert.Throws<NotSupportedException>(() => _ = new JakarJsonValueConverter<PlainDto>());
}



/// <summary> One EF Core model per configuration style (EF caches models per context type otherwise). </summary>
public sealed class ModeCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create( DbContext context, bool designTime ) => ( context.GetType(), ( (ShopContext)context ).Convention, designTime );
}
