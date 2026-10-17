# Jakar.Json.EntityFrameworkCore

Stores `[JsonModel]` properties, and arrays, lists and collection interfaces of them, in JSON columns as [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) text. It works with any EF Core provider.

```csharp
// one property
modelBuilder.Entity<Customer>().Property(x => x.Latest).HasJakarJsonConversion().HasColumnType("jsonb");

// every property of a type
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    => builder.Properties<Order>().HaveJakarJsonConversion();
```

- **`JakarJsonValueConverter<T>`** converts between the value and Jakar.Json text. Set the column type your provider uses: `jsonb` or `json` (PostgreSQL), `nvarchar(max)` (SQL Server), `TEXT` (SQLite).
- **`JakarJsonValueComparer<T>`** makes change tracking work inside the model.
  - Two values are equal when Jakar.Json writes the same text for them, which it does deterministically (dictionaries and sets are written sorted).
  - A snapshot is a deep copy.
  - So `customer.Orders.Add(...)` or `customer.Latest.Lines[0].Quantity++` is detected and saved, and assigning an equal copy saves nothing.
- `null` is stored as a `NULL` column.
- A type Jakar.Json can't serialize fails when the model is built, not at the first query.

EF Core's `ToJson()` owned-entity mapping uses EF's own JSON reader and writer, so this package doesn't change it. Use this converter for columns that hold a model as a single value.
