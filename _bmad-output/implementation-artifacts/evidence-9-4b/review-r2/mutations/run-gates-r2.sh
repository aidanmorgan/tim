#!/bin/zsh
# Review round 2 (Murdoch-2, Story 9.4b candidate 3): build, test and contract gates in the main checkout (no publish).
set -u
cd /Users/aidan/dev/personal/tim
R=_bmad-output/implementation-artifacts/evidence-9-4b/review-r2
{ echo "\$ dotnet build CuriousContraptions.slnx --no-incremental"; date -u +%FT%TZ; dotnet build CuriousContraptions.slnx --no-incremental 2>&1; echo "exit=$?"; } > $R/build-slnx.log 2>&1
: > $R/build-projects.log
for p in CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj \
  CuriousContraptions.Animation/CuriousContraptions.Animation.csproj CuriousContraptions.Abstractions.Electrical/CuriousContraptions.Abstractions.Electrical.csproj \
  CuriousContraptions.Abstractions.Fluid/CuriousContraptions.Abstractions.Fluid.csproj CuriousContraptions.Abstractions.Mechanical/CuriousContraptions.Abstractions.Mechanical.csproj \
  CuriousContraptions.Abstractions.Signal/CuriousContraptions.Abstractions.Signal.csproj elements/*/*.csproj tools/BundleElements/BundleElements.csproj \
  tools/TraceAllocations/TraceAllocations.csproj tools/Preview/Preview.csproj tools/LifecycleContract/LifecycleContract.csproj \
  tools/WireContract/WireContract.csproj tools/Ownership/Ownership.csproj; do
  out=$(dotnet build "$p" --no-incremental 2>&1); code=$?
  echo "$p exit=$code $(echo "$out" | grep -E 'Warning\(s\)|Error\(s\)' | tr -s ' ' | tr '\n' ' ')" >> $R/build-projects.log
  echo "$out" | grep -E " (warning|error) " | sort -u >> $R/build-projects.log
done
{ echo "\$ dotnet test CuriousContraptions.slnx"; date -u +%FT%TZ; dotnet test CuriousContraptions.slnx 2>&1; echo "exit=$?"; } > $R/dotnet-test.log 2>&1
{ echo "\$ node --experimental-vm-modules --test tools/*.test.mjs"; node --experimental-vm-modules --test tools/*.test.mjs 2>&1; echo "exit=$?"; } > $R/node-harnesses.log 2>&1
{ echo "\$ dotnet run --project tools/LifecycleContract"; dotnet run --project tools/LifecycleContract 2>&1; echo "exit=$?"; } > $R/lifecycle-contract.log 2>&1
{ echo "\$ dotnet run --project tools/WireContract"; dotnet run --project tools/WireContract 2>&1; echo "exit=$?"; } > $R/wire-contract.log 2>&1
{ echo "\$ dotnet run --project tools/Ownership -- --oracles ."; dotnet run --project tools/Ownership -- --oracles . 2>&1; echo "exit=$?"; } > $R/ownership-oracles.log 2>&1
echo GATES-DONE > $R/gates.done
