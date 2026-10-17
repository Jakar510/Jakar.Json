# Integrations

Where Jakar.Json plugs into the .NET ecosystem, in priority order. Every candidate with a serializer seam is now implemented: as its own package, or through [Jakar.Json.SystemTextJson](./Jakar.Json.SystemTextJson/README.md) for hosts configured with `JsonSerializerOptions`.

How each integration is built:
- **Per type.** Every integration handles `[JsonModel]` types, and arrays, lists and collection interfaces of them, through `JsonCodecRegistry` or the generated code. It leaves every other type to the host's own serializer. Hosts that want one serializer for everything get a fallback serializer.
- **Bytes where possible.** Seams that take `byte[]`, `Stream`, `IBufferWriter<byte>` or `ReadOnlySequence<byte>` are served from pooled buffers. `string` seams are transcoded once.
- **Wire compatibility.** Host envelopes, headers and content types are unchanged, so services without Jakar.Json read the payloads.

Test levels:
- **E2E** runs through the host's real pipeline: Kestrel, an in-memory transport, the client's in-memory invoker, a real ORM on SQLite, an in-process actor system, and so on.
- **Contract** tests the serializer seam directly, where the host needs infrastructure the test run doesn't have.

## Candidates, by priority

| Priority | Target | Status | Seam | Tests |
|---|---|---|---|---|
| 1 | System.Text.Json (any host) | **[Jakar.Json.SystemTextJson](./Jakar.Json.SystemTextJson/README.md)** | `JsonConverterFactory` / `JakarJsonConverter<T>` | E2E: `System.Net.Http.Json`, Microsoft.Extensions.AI, source-generated contexts, multi-segment streams |
| 2 | Microsoft.Extensions.Caching.Hybrid | **[Jakar.Json.Caching](./Jakar.Json.Caching/README.md)** | `IHybridCacheSerializerFactory` | E2E: two HybridCache instances sharing an L2 |
| 3 | HttpClient | **[Jakar.Json.Http](./Jakar.Json.Http/README.md)** | `JakarJsonContent`, `…JakarJsonAsync` extensions | E2E: Kestrel (System.Text.Json server) |
| 4 | OpenAPI | **[Jakar.Json.OpenApi](./Jakar.Json.OpenApi/README.md)** | Swashbuckle `ISerializerDataContractResolver`, `IOpenApiSchemaTransformer` | E2E: documents from both generators over Kestrel. Parity: predicted names equal what Jakar.Json writes |
| 5 | Refit | **[Jakar.Json.Refit](./Jakar.Json.Refit/README.md)** | `IHttpContentSerializer` | E2E: Kestrel |
| 6 | EF Core JSON columns | **[Jakar.Json.EntityFrameworkCore](./Jakar.Json.EntityFrameworkCore/README.md)** | `ValueConverter` + `ValueComparer` | E2E: SQLite, change tracking inside models |
| 7 | Dapper | **[Jakar.Json.Dapper](./Jakar.Json.Dapper/README.md)** | `SqlMapper.TypeHandler<T>` | E2E: SQLite |
| 8 | Confluent.Kafka | **[Jakar.Json.Kafka](./Jakar.Json.Kafka/README.md)** | `ISerializer<T>` / `IDeserializer<T>` (+ async) | E2E: producer against librdkafka's in-process mock cluster |
| 9 | AWS Lambda | **[Jakar.Json.AwsLambda](./Jakar.Json.AwsLambda/README.md)** | `ILambdaSerializer` | E2E: the runtime's `HandlerWrapper` pipeline |
| 10 | Azure SDK (`ObjectSerializer`) | **[Jakar.Json.AzureCore](./Jakar.Json.AzureCore/README.md)** | `ObjectSerializer` | E2E: Azure AI Search client over a stubbed transport |
| 11 | Azure Cosmos DB | **[Jakar.Json.Cosmos](./Jakar.Json.Cosmos/README.md)** | `CosmosLinqSerializer` | Contract, plus E2E LINQ translation (no emulator) |
| 12 | `IDistributedCache` | **[Jakar.Json.Caching](./Jakar.Json.Caching/README.md)** | extension helpers (+ `IBufferDistributedCache`) | E2E: in-memory distributed cache, both APIs |
| 13 | StackExchange.Redis | **[Jakar.Json.StackExchangeRedis](./Jakar.Json.StackExchangeRedis/README.md)** | string and hash helpers, pooled leases | Contract (in-memory `IDatabase`; no Redis server) |
| 14 | MassTransit | **[Jakar.Json.MassTransit](./Jakar.Json.MassTransit/README.md)** | System.Text.Json options | E2E: in-memory transport, publish and request/response |
| 15 | RestSharp | **[Jakar.Json.RestSharp](./Jakar.Json.RestSharp/README.md)** | `IRestSerializer` | E2E: Kestrel |
| 16 | Npgsql `json`/`jsonb` | Covered by SystemTextJson | `ConfigureJsonOptions` | Documented (needs PostgreSQL) |
| 17 | Blazor JS interop, component state | Covered by SystemTextJson | STJ options | Documented |
| 18 | Microsoft.Extensions.AI / Semantic Kernel | Covered by SystemTextJson | STJ options | E2E: `AIFunctionFactory` arguments |
| 19 | Dapr | Covered by SystemTextJson | `UseJsonSerializationOptions` | Documented (needs a sidecar) |
| 20 | Refit / RestSharp / Flurl in STJ mode | Covered by SystemTextJson | STJ options | Same converter as priority 1 |
| 21 | SignalR | **[Jakar.Json.SignalR](./Jakar.Json.SignalR/README.md)** | JSON hub protocol options | E2E: Kestrel hub and .NET client over WebSockets, plus interop with a plain client |
| 22 | Elasticsearch / OpenSearch | **[Jakar.Json.Elasticsearch](./Jakar.Json.Elasticsearch/README.md)**, **[Jakar.Json.OpenSearch](./Jakar.Json.OpenSearch/README.md)** | source serializers | E2E: in-memory invoker / connection |
| 23 | NServiceBus | **[Jakar.Json.NServiceBus](./Jakar.Json.NServiceBus/README.md)** | `SystemJsonSerializer` options | E2E: generic host, learning transport |
| 24 | Wolverine | **[Jakar.Json.Wolverine](./Jakar.Json.Wolverine/README.md)** | `IMessageSerializer` | Contract |
| 25 | Marten | **[Jakar.Json.Marten](./Jakar.Json.Marten/README.md)** | System.Text.Json serializer options + casing | Contract (no PostgreSQL) |
| 26 | EasyNetQ | **[Jakar.Json.EasyNetQ](./Jakar.Json.EasyNetQ/README.md)** | `ISerializer` (pooled `IMemoryOwner`) | Contract + DI registration (no RabbitMQ) |
| 27 | Flurl.Http | **[Jakar.Json.Flurl](./Jakar.Json.Flurl/README.md)** | `ISerializer` | E2E: Kestrel |
| 28 | DotNetCore.CAP | **[Jakar.Json.Cap](./Jakar.Json.Cap/README.md)** | System.Text.Json options | Contract (CAP's registered serializer) |
| 29 | Rebus | **[Jakar.Json.Rebus](./Jakar.Json.Rebus/README.md)** | System.Text.Json serializer options | E2E: in-memory network |
| 30 | Temporal | **[Jakar.Json.Temporal](./Jakar.Json.Temporal/README.md)** | `IEncodingConverter` (`json/plain`) | Contract (payload converter) |
| 31 | Durable Task | **[Jakar.Json.DurableTask](./Jakar.Json.DurableTask/README.md)** | `DataConverter` | Contract |
| 32 | Google Cloud Functions / Pub/Sub | Covered by SystemTextJson | STJ options | Documented |
| 33 | Orleans | **[Jakar.Json.Orleans](./Jakar.Json.Orleans/README.md)** | JSON codec for exactly the supported types | E2E: Orleans' configured `Serializer` |
| 34 | Akka.NET | **[Jakar.Json.Akka](./Jakar.Json.Akka/README.md)** | `Serializer` + setup / HOCON | E2E: actor system with `serialize-messages = on` |
| 35 | Serilog | **[Jakar.Json.Serilog](./Jakar.Json.Serilog/README.md)** | `IDestructuringPolicy` | E2E: logger with a collecting sink |
| 36 | Microsoft.Extensions.Logging | **[Jakar.Json.Logging](./Jakar.Json.Logging/README.md)** | `ConsoleFormatter` | Contract + DI registration |
| 37 | ASP.NET Core TempData | **[Jakar.Json.AspNetCore](./Jakar.Json.AspNetCore/README.md)** 10.2.0 | `TempDataSerializer` decorator | E2E: cookie TempData across a redirect over Kestrel |
| 38 | GraphQL.Client | **[Jakar.Json.GraphQLClient](./Jakar.Json.GraphQLClient/README.md)** | System.Text.Json serializer options | E2E: HTTP client over a stub server |
| 39 | Quartz.NET | **[Jakar.Json.Quartz](./Jakar.Json.Quartz/README.md)** | job-data helpers (Quartz stores structured values as strings) | Contract, through Quartz's persistent-store serializer |
| 40 | gRPC JSON transcoding | Covered by SystemTextJson | STJ options | Documented |
| 41 | HotChocolate | Covered by SystemTextJson | STJ options | Documented |

### Notes from implementing

- **OpenAPI** reads `[JsonModel]` settings at run time. Jakar.Json's attributes are `[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]`, so the package defines that symbol through `buildTransitive`, and model assemblies elsewhere need it too. If the generator emitted member metadata into the registry, that requirement would go away.
- **Quartz** rejects structured job-data values by design, so the integration stores models as Jakar.Json text instead of replacing Quartz's serializer.
- **Temporal:** the payload converter keys encoding converters by name. Jakar.Json's converter replaces `json/plain` (falling back to it) rather than adding a second one.
- **The MassTransit, NServiceBus, CAP, Marten, Rebus, Orleans, GraphQL.Client and SignalR integrations** go through the host's System.Text.Json serializer. Those hosts own an envelope, headers and type naming that a raw serializer would have to re-implement. The model inside the envelope is still Jakar.Json's.

## No seam (not implementable)

| Target | Why |
|---|---|
| Hangfire | Serialization is fixed to Newtonsoft.Json settings; there's no serializer abstraction to replace. |
| ASP.NET Core output caching | Caches the response bytes the endpoint already wrote (by Jakar.Json, when it uses Jakar.Json.AspNetCore) and replays them without deserializing. A replacement middleware would cache the same bytes, so a serializer has nothing to do. Caching the model instead belongs in HybridCache or FusionCache. |
| Microsoft.Extensions.Configuration | Configuration binding, not JSON deserialization. |
| YARP | Proxies bytes without deserializing them. |
| MongoDB driver | BSON, not JSON. |
| MessagePack-CSharp / MemoryPack / protobuf-net | Competing binary formats. |
| Polly, MediatR, OpenTelemetry | No serialization. |
