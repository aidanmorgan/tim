#!/usr/bin/env bash
# Verbatim body of the "Test C# tools" step in .github/workflows/pages.yml at main f9ce61ef, run under bash as GitHub Actions does.
set -eo pipefail
          dotnet test tools/Coverage.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          dotnet test tools/BundleElements.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          dotnet test tools/WorkerAotGuard.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse
          deferred=$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)
          dotnet test tools/E2eRunner.Tests --nologo --no-build --no-restore -c Debug -f net10.0 -noAutoResponse --filter "$deferred"
