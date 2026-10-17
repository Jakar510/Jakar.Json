# Jakar.Json.Dapper

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) type handlers for [Dapper](https://github.com/DapperLib/Dapper).

- `[JsonModel]` parameters, and arrays, lists and collection interfaces of them, are written as Jakar.Json text.
- JSON columns, stored as text or as UTF-8 bytes, are read back into models.
- It works with any ADO.NET provider.

```csharp
JakarJsonDapper.AddTypeHandler<Order>();
JakarJsonDapper.AddTypeHandler<List<Order>>();
// or every [JsonModel] type in an assembly (uses reflection; not for trimmed apps)
JakarJsonDapper.AddTypeHandlers(typeof(Order).Assembly);

connection.Execute("INSERT INTO Customers (Name, Latest) VALUES (@Name, @Latest)", new { Name = "Zoë", Latest = order });
Customer customer = connection.QuerySingle<Customer>("SELECT * FROM Customers WHERE Id = @Id", new { Id = 1 });
```

- **Registration is process-wide:** Dapper's type handlers are global, so register them once at startup.
- **Collections:** a `List<Order>` parameter with a handler is stored as one JSON value. Dapper doesn't expand it into an `IN (...)` list.
- **`null`** values are stored as `NULL`, and `NULL` columns read back as `null`.
- **Parameters** are sent as `DbType.String`. For PostgreSQL `jsonb`, cast in SQL (`@Latest::jsonb`) or use a `jsonb` column with an implicit cast.
