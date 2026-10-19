#!/bin/zsh
# Murdoch-6 Tier 1 on the clean 7a8a48e4 worktree.
W=/Users/aidan/dev/personal/tim/.claude/worktrees/murdoch6-9r0b
OUT=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/ev/tier1
mkdir -p $OUT
cd $W
run() { local name=$1; shift; local s=$(date +%s); "$@" > $OUT/$name.log 2>&1; local rc=$?; echo "$name exit=$rc secs=$(( $(date +%s) - s ))" | tee -a $OUT/summary.txt; }
echo "head=$(git rev-parse HEAD) status=$(git status --porcelain | wc -l)" > $OUT/summary.txt
run build dotnet build CuriousContraptions.slnx --nologo
run build-playtest dotnet build CuriousContraptions.slnx --nologo -p:PlaytestDiagnostics=true
run test dotnet test CuriousContraptions.slnx --nologo
run node-harnesses node --experimental-vm-modules --test tools/*.test.mjs
run lifecycle-contract dotnet run --project tools/LifecycleContract
run wire-contract dotnet run --project tools/WireContract
run ownership dotnet run --project tools/Ownership
run anvil anvil check --changed
echo "status-after=$(git status --porcelain | wc -l)" >> $OUT/summary.txt
