// dotnet run -c Release -- [--bench] [BenchmarkDotNet arguments, e.g. --filter *JakarJson*]
// dotnet run -c Release -- --profile [JakarJson_Benchmarks method] [lines] [seconds]   (a hot loop for dotnet-trace; see JsonProfile)

if ( args is ["--profile", ..] )
{
    Jakar.Json.Benchmarks.JsonProfile.Run(args[1..]);
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(JakarJson_Benchmarks).Assembly)
                 .Run(args is ["--bench", ..]
                          ? args[1..]
                          : args);
