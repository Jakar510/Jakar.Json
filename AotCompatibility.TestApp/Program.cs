// Native AOT smoke test for Jakar.Json: every check runs in the published native executable; any failure exits non-zero.
// dotnet publish AotCompatibility.TestApp -c Release -r win-x64 -warnaserror, then run the published exe (it must exit 0).

using AotCompatibility.TestApp;


int failures = 0;

foreach ( ( string name, Func<bool> test ) in JakarJsonChecks.All() )
{
    try
    {
        bool ok = test();
        Console.WriteLine($"{( ok ? "PASS" : "FAIL" )}  {name}");
        if ( !ok ) { failures++; }
    }
    catch ( Exception e )
    {
        Console.WriteLine($"FAIL  {name}: {e.GetType().Name}: {e.Message}");
        failures++;
    }
}

Console.WriteLine(failures == 0
                      ? "All checks passed."
                      : $"{failures} check(s) failed.");

return failures == 0
           ? 0
           : 1;
