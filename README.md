# Jakar.Json

A reflection-free, allocation-free JSON serializer for .NET 10, with integrations for the frameworks and libraries .NET apps serialize through. **Contributions and ideas are welcome.**

| Package | What it is |
|---|---|
| [Jakar.Json](./Jakar.Json/README.md) | `[JsonModel]` source-generates UTF-16 and UTF-8 readers and writers. It also includes a JSON tape, a DOM, and System.Text.Json models. It ships its source generator and code fixes. The design is in [SPEC.md](./Jakar.Json/SPEC.md). |

Jakar.Json depends on [Jakar.Spans](https://github.com/Jakar510/Jakar.Spans); [Jakar.Extensions](https://github.com/Jakar510/Jakar.Extensions) builds on Jakar.Json.

## Integrations

Each integration reads and writes `[JsonModel]` types, and arrays, lists and collection interfaces of them, with Jakar.Json. Other types are left to the host's own serializer, so you can adopt it one model at a time. [INTEGRATIONS.md](./INTEGRATIONS.md) has the full survey, including the hosts with no serializer seam.

| Area | Packages |
|---|---|
| **System.Text.Json hosts** | [SystemTextJson](./Jakar.Json.SystemTextJson/README.md): one converter for every framework configured through `JsonSerializerOptions`, such as Npgsql, Blazor, Dapr and Microsoft.Extensions.AI |
| **ASP.NET Core** | [AspNetCore](./Jakar.Json.AspNetCore/README.md) (MVC, minimal APIs, TempData) · [OpenApi](./Jakar.Json.OpenApi/README.md) (Swashbuckle, Microsoft.AspNetCore.OpenApi) · [SignalR](./Jakar.Json.SignalR/README.md) |
| **HTTP clients** | [Http](./Jakar.Json.Http/README.md) (`HttpClient`) · [Refit](./Jakar.Json.Refit/README.md) · [RestSharp](./Jakar.Json.RestSharp/README.md) · [Flurl](./Jakar.Json.Flurl/README.md) · [GraphQLClient](./Jakar.Json.GraphQLClient/README.md) |
| **Caching** | [Caching](./Jakar.Json.Caching/README.md) (HybridCache, `IDistributedCache`) · [FusionCache](./Jakar.Json.FusionCache/README.md) · [StackExchangeRedis](./Jakar.Json.StackExchangeRedis/README.md) |
| **Data** | [EntityFrameworkCore](./Jakar.Json.EntityFrameworkCore/README.md) · [Dapper](./Jakar.Json.Dapper/README.md) · [Marten](./Jakar.Json.Marten/README.md) · [Cosmos](./Jakar.Json.Cosmos/README.md) · [Elasticsearch](./Jakar.Json.Elasticsearch/README.md) · [OpenSearch](./Jakar.Json.OpenSearch/README.md) |
| **Messaging** | [Kafka](./Jakar.Json.Kafka/README.md) · [MassTransit](./Jakar.Json.MassTransit/README.md) · [NServiceBus](./Jakar.Json.NServiceBus/README.md) · [Wolverine](./Jakar.Json.Wolverine/README.md) · [Rebus](./Jakar.Json.Rebus/README.md) · [EasyNetQ](./Jakar.Json.EasyNetQ/README.md) · [Cap](./Jakar.Json.Cap/README.md) |
| **Cloud and workflows** | [AzureCore](./Jakar.Json.AzureCore/README.md) (Azure SDK `ObjectSerializer`) · [AwsLambda](./Jakar.Json.AwsLambda/README.md) · [Temporal](./Jakar.Json.Temporal/README.md) · [DurableTask](./Jakar.Json.DurableTask/README.md) · [Quartz](./Jakar.Json.Quartz/README.md) |
| **Actors** | [Orleans](./Jakar.Json.Orleans/README.md) · [Akka](./Jakar.Json.Akka/README.md) |
| **Logging** | [Serilog](./Jakar.Json.Serilog/README.md) · [Logging](./Jakar.Json.Logging/README.md) (Microsoft.Extensions.Logging JSON console) |

## Repository

| Project | Purpose |
|---|---|
| `Jakar.Json.Generator`, `Jakar.Json.Generator.CodeFixes` | The source generator, analyzers and code fixes (packed into Jakar.Json). |
| `Jakar.Json.<Host>` | The integration packages. Most import `Shared/Integration.props`, which provides the package metadata, the Jakar.Json reference and the shared helpers in `Shared/*.cs`. |
| `Jakar.Json.Tests`, `Jakar.Json.Generator.Tests`, `Jakar.Json.<Host>.Tests` | NUnit test suites. The integration suites import `Shared/Tests.props` and share the models in `Shared/TestModels`. |
| `Jakar.Json.Tests.WebApi` | A sample ASP.NET Core app (MVC and minimal APIs) using Jakar.Json.AspNetCore. |
| `Jakar.Json.Benchmarks` | BenchmarkDotNet: Jakar.Json vs System.Text.Json vs Newtonsoft.Json (`--filter *JakarJson*`), and a `--profile` loop for dotnet-trace. |
| `AotCompatibility.TestApp` | Checks that Jakar.Json publishes with Native AOT and full trimming without warnings. |

Jakar.Spans comes from NuGet. Until it's on nuget.org, `nuget.config` also reads the local feed `W:\WorkSpace\LocalNuGet`.

```sh
dotnet test Jakar.Json.slnx -c Release
dotnet pack Jakar.Json/Jakar.Json.csproj -c Release -o nupkg
dotnet pack Jakar.Json.Kafka/Jakar.Json.Kafka.csproj -c Release -o nupkg   # any package
```

Every push to `master` publishes all packages whose version isn't on nuget.org yet (`.github/workflows/publish.yml`).

## Versioning

Versions are **`<.NET major>.<library major>.<library minor>`**:

| Part | Meaning |
|---|---|
| .NET major | The .NET version the package targets (10 for .NET 10). |
| Library major | Breaking changes: removed or changed public API, or a different wire format. |
| Library minor | New features and fixes that don't break existing code. |

A new package starts at `<.NET major>.1.0`. Release order: Jakar.Spans, then Jakar.Json, then Jakar.Json.SystemTextJson and Jakar.Json.Http, then the other integrations.

## License

MIT. See [LICENSE.txt](./LICENSE.txt).
