# Jakar.Json.Wolverine

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as [Wolverine](https://wolverinefx.net)'s message serializer.

```csharp
builder.Host.UseWolverine(opts => opts.UseJakarJson());
```

- **Models:** messages of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written by Jakar.Json and read straight from the envelope's body.
- **Other types** go to Wolverine's previous default serializer (System.Text.Json).
- **Content type** stays `application/json`, so other Wolverine services interoperate.
- Reading data without a message type (`ReadFromData(byte[])`) is left to the fallback: Jakar.Json needs the type to pick a codec.
