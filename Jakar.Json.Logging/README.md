# Jakar.Json.Logging

A JSON console formatter for `Microsoft.Extensions.Logging` that writes `[JsonModel]` values as structured JSON, using [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md).

```csharp
builder.Logging.AddJakarJsonConsole(o => o.IncludeScopes = true);

logger.LogInformation("Placed {Order}", order);
```

```json
{"Timestamp":"...","EventId":0,"LogLevel":"Information","Category":"Orders","Message":"Placed Order { Id = 7, ... }",
 "State":{"Message":"...","Order":{"id" : 7,"customer" : "Zoë","total" : 12.50,"lines" : [...]},"{OriginalFormat}":"Placed {Order}"}}
```

- **Shape:** each entry has the same fields as the built-in `JsonConsoleFormatter`. Log pipelines that read it keep working.
- **Models:** `[JsonModel]` values in the state and scopes, and arrays, lists and collection interfaces of them, are JSON objects and arrays written by Jakar.Json, not their `ToString()`.
- **Other values:** numbers and booleans stay JSON numbers and booleans, and other values are written as their invariant text. NaN and infinities are written as text, so a log line is always valid JSON.
- **Options:** the console formatter's (`IncludeScopes`, `TimestampFormat`, `UseUtcTimestamp`), plus `Indented` and `Escaping`. The whole entry is written by Jakar.Json; System.Text.Json isn't involved.

Registration binds options from configuration, as `AddConsoleFormatter` does, so it's annotated as not trimming-safe.
