# Jakar.Json

A reflection-free, allocation-free JSON serializer for .NET 10, with ASP.NET Core integration. **Contributions and ideas are welcome.**

| Package | What it is |
|---|---|
| [Jakar.Json](./Jakar.Json/README.md) | `[JsonModel]` source-generates UTF-16 and UTF-8 readers and writers; also a JSON tape, a DOM, and System.Text.Json models. Ships its source generator and code fixes. The design is in [SPEC.md](./Jakar.Json/SPEC.md). |
| [Jakar.Json.AspNetCore](./Jakar.Json.AspNetCore/README.md) | Jakar.Json as ASP.NET Core's serializer for `[JsonModel]` types: MVC formatters, and minimal API results, request bodies and an endpoint filter. |

Jakar.Json depends on [Jakar.Spans](https://github.com/Jakar510/Jakar.Spans); [Jakar.Extensions](https://github.com/Jakar510/Jakar.Extensions) builds on Jakar.Json.

## Repository

| Project | Purpose |
|---|---|
| `Jakar.Json.Generator`, `Jakar.Json.Generator.CodeFixes` | The source generator, analyzers and code fixes (packed into Jakar.Json). |
| `Jakar.Json.Tests`, `Jakar.Json.Generator.Tests`, `Jakar.Json.AspNetCore.Tests` | NUnit test suites. |
| `Jakar.Json.Tests.WebApi` | A sample ASP.NET Core app (MVC and minimal APIs) using Jakar.Json.AspNetCore. |
| `Jakar.Json.Benchmarks` | BenchmarkDotNet: Jakar.Json vs System.Text.Json vs Newtonsoft.Json (`--filter *JakarJson*`), and a `--profile` loop for dotnet-trace. |
| `AotCompatibility.TestApp` | Checks that Jakar.Json publishes with Native AOT and full trimming without warnings. |

Jakar.Spans comes from NuGet. Until it's on nuget.org, `nuget.config` also reads the local feed `W:\WorkSpace\LocalNuGet`.

```sh
dotnet test Jakar.Json.slnx -c Release
dotnet pack Jakar.Json/Jakar.Json.csproj -c Release -o nupkg
dotnet pack Jakar.Json.AspNetCore/Jakar.Json.AspNetCore.csproj -c Release -o nupkg
```

## Versioning

Versions are **`<.NET major>.<library major>.<library minor>`**:

| Part | Meaning |
|---|---|
| .NET major | The .NET version the package targets (10 for .NET 10). |
| Library major | Breaking changes: removed or changed public API, or a different wire format. |
| Library minor | New features and fixes that don't break existing code. |

A new package starts at `<.NET major>.1.0`: Jakar.Json and Jakar.Json.AspNetCore started at 10.1.0. Release order: Jakar.Spans, then Jakar.Json, then Jakar.Json.AspNetCore.

## License

MIT. See [LICENSE.txt](./LICENSE.txt).
