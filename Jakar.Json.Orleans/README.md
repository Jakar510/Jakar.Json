# Jakar.Json.Orleans

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [Orleans](https://learn.microsoft.com/dotnet/orleans).

```csharp
siloBuilder.Services.AddSerializer(s => s.AddJakarJsonSerializer());
// and on clients
clientBuilder.Services.AddSerializer(s => s.AddJakarJsonSerializer());
```

- **Models:** grain call arguments, results and grain state of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written by Jakar.Json inside Orleans' JSON codec. The routing is exact: the codec takes the types Jakar.Json supports, and no others.
- **Every other type** keeps Orleans' own serializers, including `[GenerateSerializer]` types and the built-in types.
- **When to use it:** Orleans' generated binary codecs are already fast. This fits models shared with JSON stores and services, where one model definition and one wire format pay off.

Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
