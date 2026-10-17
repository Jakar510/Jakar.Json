# Jakar.Json.Temporal

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for the [Temporal .NET SDK](https://github.com/temporalio/sdk-dotnet).

```csharp
TemporalClient client = await TemporalClient.ConnectAsync(new("localhost:7233")
{
    DataConverter = DataConverter.Default.WithJakarJson(),
});
```

Use the same data converter for workers.

- **Models:** workflow and activity arguments, results, signals and queries of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json.
- **Encoding:** these payloads keep the `json/plain` encoding. `JakarJsonPlainConverter` takes the place of Temporal's `JsonPlainConverter`, so workers without Jakar.Json, and the Temporal UI, read them as usual.
- **Other values** keep Temporal's converters: nulls, `byte[]`, protobuf messages, and System.Text.Json for every other type.
- `WithJakarJson()` keeps the converter's failure converter and payload codec, so encryption codecs still apply.
