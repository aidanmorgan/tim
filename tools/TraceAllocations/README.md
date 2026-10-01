# Native allocation trace report

Reads one EventPipe .nettrace file and writes JSON for the 30 largest sampled allocation sites, grouped by typed managed-type and call-stack identities. Byte totals sum GC allocation-tick estimates; they are not exact per-type allocations, CPU costs, or browser FPS measurements. Lost events, malformed input and incorrect argument counts fail explicitly.

```sh
dotnet build tools/TraceAllocations -c Release
dotnet tools/TraceAllocations/bin/Release/net10.0/TraceAllocations.dll capture.nettrace
```

The reader creates a sibling .etlx index. Use a writable copy of a retained trace. The TraceEvent dependency is pinned to 3.1.23; conversion uses CreateFromEventPipeDataFile, not the ETW loader.

For a repeatable capture, install dotnet-trace into a local tool directory, then run a prebuilt focused test assembly under `dotnet-trace collect --profile dotnet-sampled-thread-time,gc-verbose --output capture.nettrace --show-child-io -- dotnet path/to/tests.dll`. Keep the exact test selection and source revision with the trace. Profiling adds overhead; do not compare its elapsed time directly with an untraced test run.

Verification on 1 October 2026: Release build succeeds with no warnings; the retained native Cannon trace was read successfully and reproduced the development report byte-for-byte. The trace contains both powered and disconnected-supply modes. This verifies report reproduction, not browser performance or component qualification. Native traces and browser captures remain separate evidence.

