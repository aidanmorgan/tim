#!/bin/zsh
# Murdoch-6 red publish: red.sh <label> <msbuild args...>. Publishes the Simulation worker in the red worktree.
W=/Users/aidan/dev/personal/tim/.claude/worktrees/murdoch6-red
OUT=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/ev/red
mkdir -p $OUT
label=$1; shift
cd $W
s=$(date +%s)
dotnet publish CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj -c Release --nologo "$@" > $OUT/$label.log 2>&1
rc=$?
{
  echo "label=$label args=$* exit=$rc seconds=$(( $(date +%s) - s )) head=$(git rev-parse --short HEAD) targets_sha256=$(shasum -a 256 CuriousContraptions.web/WorkerAot.targets | cut -c1-16)"
  git diff --stat | tail -1
  grep -E "error|WORKER_AOT_SIMD" $OUT/$label.log | sed 's/^ *//' | sort -u | cut -c1-400
} > $OUT/$label.summary.txt
cat $OUT/$label.summary.txt
