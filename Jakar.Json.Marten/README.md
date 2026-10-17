# Jakar.Json.Marten

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [Marten](https://martendb.io), the PostgreSQL document database and event store.

```csharp
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.UseJakarJson();                 // camelCase, as [JsonModel(Naming = JsonNaming.CamelCase)]
});
```

- **Models:** documents and events of `[JsonModel]` types are written to and read from `jsonb` by Jakar.Json, through Marten's System.Text.Json serializer.
- **Other types** keep System.Text.Json.
- **LINQ:** Marten builds JSON paths from its own casing setting, not from your models. Pass the casing your models are written with:
  - `Casing.CamelCase` (the default here) for camelCase;
  - `Casing.Default` for names as declared;
  - `Casing.SnakeCase` for snake_case.

  Otherwise, queries look for properties the documents don't have.
