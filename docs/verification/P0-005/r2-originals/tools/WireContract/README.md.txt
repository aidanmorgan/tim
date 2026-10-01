# P0-005 design fixtures

Standalone .NET10 executable; the game excludes tools/**/*.cs and never references this project.
It validates the frozen design's envelope schema subset, enum routes, exact integer byte goldens,
length arithmetic, stable-ID versus local-slot maps and timed application ordering.
It is not the P0-016 production codec, transport implementation or runtime replay system.

```sh
dotnet build tools/WireContract/WireContract.csproj -c Release
dotnet tools/WireContract/bin/Release/net10.0/WireContract.dll
```

Exit0 means all enumerated design fixtures passed; exit1 means a fixture failed.
The JSON result uses the Oracle enum's explicit source-defined numeric case identities.
RuntimeQualified is always false. Full lifecycle/duplicate/ack oracles are hand-derived in
[the contract](../../docs/work-orders/P0-005/replay.md), not falsely reported as exercised worker code.
P0-016 independently implements/tests exact C#/JS codecs; P0-032 attacks real workers and replay.

R2 adds32 focused descriptor/budget/identity/window controls (82 total). Exact R1 originals and
its independent Fail are retained. New controls are design models, not worker execution evidence.
