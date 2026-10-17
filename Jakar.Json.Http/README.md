# Jakar.Json.Http

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for `HttpClient`, in the shape of `System.Net.Http.Json`.

```csharp
Order? order = await client.GetFromJakarJsonAsync<Order>("orders/7");

using HttpResponseMessage response = await client.PostAsJakarJsonAsync("orders", order);
Order? saved = await response.Content.ReadFromJakarJsonAsync<Order>();
```

| Method | Does |
|---|---|
| `GetFromJakarJsonAsync<T>` / `DeleteFromJakarJsonAsync<T>` | Sends the request, throws `HttpRequestException` on a non-success status, and reads the body. |
| `PostAsJakarJsonAsync` / `PutAsJakarJsonAsync` / `PatchAsJakarJsonAsync` | Sends the value as a Jakar.Json body and returns the response. |
| `content.ReadFromJakarJsonAsync<T>` | Reads any response's body. |
| `JakarJsonContent.Create(value)` | The `HttpContent` itself, for `HttpRequestMessage` and other APIs. |

- **Types:** `[JsonModel]` types, and arrays, lists and collection interfaces of them.
- **Request bodies** are written from a pooled buffer, as `application/json; charset=utf-8`. Like `JsonContent`, the length isn't known in advance.
- **Response bodies** are streamed and parsed in one pass. Bodies in another charset (UTF-16, Latin-1, ...) are transcoded first. A `null` body reads as `default`.
- **Errors:** malformed JSON throws `JsonReadException`, which derives from `JsonException`.

For Refit, RestSharp or Flurl, see [Jakar.Json.Refit](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.Refit/README.md), [Jakar.Json.RestSharp](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.RestSharp/README.md) and [Jakar.Json.Flurl](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.Flurl/README.md).
