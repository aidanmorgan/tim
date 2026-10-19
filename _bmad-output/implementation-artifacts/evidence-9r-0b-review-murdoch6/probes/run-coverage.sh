#!/bin/zsh
# Runs the AOT coverage probe over every CuriousContraptions.* assembly of both workers in the murdoch6 publish.
W=/Users/aidan/dev/personal/tim/.claude/worktrees/murdoch6-9r0b
P=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/probe
OUT=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/ev/aot-coverage
NM=$(ls ~/.dotnet/packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.osx-arm64/*/tools/bin/llvm-nm | head -1)
mkdir -p $OUT
for worker in CuriousContraptions.Simulation CuriousContraptions.Animation.Worker; do
  D=$W/$worker/obj/Release/net10.0/browser-wasm/wasm/for-publish
  for o in $D/CuriousContraptions.*.dll.o $D/aot-instances.dll.o; do
    $NM --defined-only $o > $OUT/$worker.$(basename $o).nm 2>&1
  done
  for asm in $D/aot-in/CuriousContraptions.*.dll; do
    base=$(basename $asm .dll)
    tok=$D/tokens/${base//./_}_dll_compiled_methods.txt
    dotnet run --file $P/AotCoverage.cs -- $asm $tok $OUT/$worker.$base.dll.o.nm $OUT/$worker.aot-instances.dll.o.nm > $OUT/$worker.$base.coverage.txt 2>&1
    tail -1 $OUT/$worker.$base.coverage.txt
  done
done
