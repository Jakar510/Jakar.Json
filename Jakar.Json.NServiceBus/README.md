# Jakar.Json.NServiceBus

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [NServiceBus](https://particular.net/nservicebus).

```csharp
EndpointConfiguration endpoint = new("Sales");
endpoint.UseJakarJsonSerialization();
builder.Services.AddNServiceBusEndpoint(endpoint);
```

- **Models:** messages of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json, through NServiceBus' `SystemJsonSerializer`.
- **Everything else** keeps System.Text.Json: headers and every other type. Endpoints without Jakar.Json still read the messages, since the content type is unchanged.
- Pass `UseJakarJsonSerialization(options)` to configure the System.Text.Json side.

NServiceBus is commercially licensed for production use. Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
