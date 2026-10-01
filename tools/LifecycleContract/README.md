# P0-006 design fixtures

Standalone .NET10 design-only executable with no runtime reference. The game excludes tools/**/*.cs.
It checks finite lifecycle schema/mode decisions, failure-oracle classification, numerical clock
and combined history bounds, and stable identity resolution. It does not implement a worker,
transaction, save API, UI controller or clock mapper.

```sh
dotnet build tools/LifecycleContract/LifecycleContract.csproj -c Release --nologo
dotnet tools/LifecycleContract/bin/Release/net10.0/LifecycleContract.dll
```

Exit0 means all enumerated design checks passed; exit1 retains the failed case and error.
Output uses typed enum case IDs and mode/command/failure/mutation identities.
RuntimeQualified is always false. Unknown/missing/duplicate JSON fields and unsupported enum/
step values reject at the explicit external schema boundary. Semantic runtime failure injection
belongs to P0-014/020/032; Chrome UI/build/performance proof is not replaced by this tool.

