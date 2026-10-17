# Jakar.Json.Refit

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as [Refit](https://github.com/reactiveui/refit)'s content serializer.

```csharp
IOrdersApi api = RestService.For<IOrdersApi>(httpClient, new RefitSettings().UseJakarJson());

// or with IHttpClientFactory
services.AddRefitClient<IOrdersApi>(new RefitSettings().UseJakarJson());
```

- Request and response bodies of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json. Requests use [Jakar.Json.Http](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.Http/README.md)'s `JakarJsonContent`, and responses are streamed.
- Every other type goes to the fallback: the serializer the settings already had, or Refit's `SystemTextJsonContentSerializer`. You can pass your own with `new JakarJsonContentSerializer(fallback)`.
- Query-string and form field names come from the fallback, because Refit asks the content serializer for them.

Refit's errors behave as usual: `ApiException`, and `IApiResponse<T>` for non-success statuses.
