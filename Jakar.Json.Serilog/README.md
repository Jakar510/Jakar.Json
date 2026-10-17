# Jakar.Json.Serilog

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) destructuring for [Serilog](https://serilog.net).

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.UsingJakarJson()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

Log.Information("Placed {@Order}", order);
```

- **Models:** `[JsonModel]` values, and arrays, lists and collection interfaces of them, logged with `{@...}` are destructured from what Jakar.Json writes. The property names, member order, ignored members and nulls are the same as in your API's JSON. There's no reflection.
- **Shape:** objects become `StructureValue`s tagged with the model's type name, and arrays become `SequenceValue`s.
- **Numbers:** integers become `long`; other numbers become `decimal` when they fit (keeping the written digits), otherwise `double`.
- **Other types** keep Serilog's destructuring.
