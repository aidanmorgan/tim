# P0-006 design fixtures

The [current engine contract](../../docs/engine-contracts.md) governs runtime applicability: GPU completion/validation defines commit, device loss is a typed fault, and construction saves retain canonical half bits. Frozen lifecycle fixtures and host-clock controls do not establish those GPU behaviors.

Standalone .NET10 executable with source-linked Workshop clock controls and no runtime project reference. The game excludes tools/**/*.cs.
It checks finite lifecycle schema/mode decisions, failure-oracle classification, combined history bounds,
stable identity resolution, and the actual Workshop clock mapping/native conversion source. It does not
run a worker, transaction, save API or UI controller, or qualify an actual platform clock.

```sh
dotnet build tools/LifecycleContract/LifecycleContract.csproj -c Release --nologo
dotnet tools/LifecycleContract/bin/Release/net10.0/LifecycleContract.dll
```

Exit0 means all enumerated design checks passed; exit1 retains the failed case and error.
Output uses typed enum case IDs and mode/command/failure/mutation identities.
RuntimeQualified is always false. Unknown/missing/duplicate JSON fields and unsupported enum/
step values reject at the explicit external schema boundary. Semantic runtime failure injection
belongs to P0-014/020/032; Chrome UI/build/performance proof is not replaced by this tool.
