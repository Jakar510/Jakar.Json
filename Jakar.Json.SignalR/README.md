# Jakar.Json.SignalR

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for SignalR's JSON hub protocol.

```csharp
// server
builder.Services.AddSignalR().AddJakarJsonProtocol();

// .NET client (MAUI, WPF, console, other services)
HubConnection connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/hubs/orders")
    .AddJakarJsonProtocol()
    .Build();
```

- **Models:** hub method arguments, return values, streams and server-to-client messages of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json.
- **Everything else** keeps System.Text.Json: other types and the protocol's own framing.
- **Compatibility:** the protocol's name is still `json`, so JavaScript clients, and .NET clients and servers without Jakar.Json, keep working against it.
- **Native AOT:** list the types with `AddJakarJsonProtocol(o => o.PayloadSerializerOptions.AddJakarJson(f => f.Add<Order>()))`.

It's built on the standalone `Microsoft.AspNetCore.SignalR.Protocols.Json` package, so .NET clients don't need the ASP.NET Core framework. Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
