#!/bin/zsh
# Story 9A.3 Tier 1 on the committed candidate, run from the worktree root. Each step's summary lines go to stdout; each step's full
# output goes to <evidence>/tier1/<step>.log. Usage: zsh tier1.sh > <evidence>/tier1.txt
E=_bmad-output/implementation-artifacts/evidence-9a-3
L=$E/tier1; mkdir -p $L
echo "HEAD $(git rev-parse HEAD); status digest $(git status --porcelain -- . ':(exclude)_bmad-output' | shasum -a 256 | cut -c1-12) (excluding evidence)"
step() {
  name=$1; shift
  "$@" > "$L/$name.log" 2>&1; rc=$?
  echo "### $name: exit=$rc"
  grep -E "Warning\(s\)|Error\(s\)|Passed!|Failed!|: error |: warning |\"Failures\"|\"Count\"|No warnings|Checked " "$L/$name.log" | sed -E 's#/Users/[^ ]*/agent-a71abbddd0caa4be4/##g' | sort -u | head -12
}
step build-debug dotnet build CuriousContraptions.slnx -nologo
step build-playtest dotnet build CuriousContraptions.slnx -nologo -p:PlaytestDiagnostics=true
step build-release dotnet build CuriousContraptions.slnx -nologo -c Release
step build-web dotnet build CuriousContraptions.web/CuriousContraptions.web.csproj -nologo -p:WasmBuildOnlyAfterPublish=true -p:TwoDogAutoImport=false
step build-debug-again dotnet build CuriousContraptions.slnx -nologo
step test dotnet test CuriousContraptions.slnx -nologo --no-build
step node zsh $E/tier1-node.sh
step lifecycle-contract dotnet run --project tools/LifecycleContract -c Release
step wire-contract dotnet run --project tools/WireContract -c Release
step ownership-oracles dotnet run --project tools/Ownership -c Release -- --oracles .
step anvil-check anvil check --since 379cc24a
grep -E "exit=" "$L/node.log" | grep -v "exit=0" && echo "node: a harness failed" || echo "node: every harness exit=0"
echo DONE
