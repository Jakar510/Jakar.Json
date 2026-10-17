# Jakar.Json.AwsLambda

An AWS Lambda serializer on [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md).

- Requests and responses of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are read and written without reflection.
- Every other type goes to a fallback serializer. That covers the AWS event types (API Gateway, SQS, ...).

```csharp
// Native AOT friendly: the fallback is System.Text.Json's source-generated Lambda serializer.
[assembly: LambdaSerializer(typeof(JakarJsonLambdaSerializer<SourceGeneratorLambdaJsonSerializer<MyJsonContext>>))]

// Or, with reflection-based System.Text.Json for other types:
[assembly: LambdaSerializer(typeof(JakarJsonLambdaSerializer))]
```

You can also pass it to the runtime client: `LambdaBootstrapBuilder.Create(handler, new JakarJsonLambdaSerializer<...>())`.

- **Native AOT:** Lambda's fast cold starts are a natural fit for a reflection-free serializer. With a source-generated fallback, the whole function stays AOT and trimming compatible. The parameterless `JakarJsonLambdaSerializer` uses `DefaultLambdaJsonSerializer` (reflection) for other types, and is annotated as such.
- **Requests** are read into a pooled buffer and parsed in one pass. Responses are written from a pooled buffer.
- **Errors:** malformed requests throw `JsonReadException`.
