# Ownership inspection (P0-004 Design)

Build with `dotnet build tools/Ownership/Ownership.csproj -c Release`.
Run `dotnet tools/Ownership/bin/Release/net10.0/Ownership.dll --oracles` for the typed positive
and rejection cases. This executable uses the pinned SDK's Roslyn assemblies, without a new package.

`--audit . docs/work-orders/P0-004/ownership/index.json` rebuilds the real production/test
projects in Release and PLAYTEST contexts, captures actual generated binding sources under this
tool's ignored obj/binding directory, resolves compiler references and checks the explicit
source/member/writer fingerprints and selected source-derived ownership guards. Generated binding
output is disposable and cleared before each context capture. This command does not run native tests,
export the browser bundle, drive Chrome, or qualify runtime behavior.

`--members .` emits metadata/fingerprints and context identities without full use/call arrays.
`--capture .` emits the reproducible full use/call census for independent queries. The checked-in
ownership shards retain fingerprints rather than duplicate that graph. Numeric JSON discriminants
are an external representation of the enum-typed C# model; unknown values and fields reject.
Invalid boundary input exits 1 with an error; stale/missing evidence remains Incomplete in review
even though the tool uses the same nonzero exit code.

Read [the contract](../../docs/work-orders/P0-004/contract.md) and
[analysis bounds](../../docs/work-orders/P0-004/analysis-bounds.md) before interpreting the results.
One assignment is required per authored member. The full current writer/caller context is
conservative, not a points-to proof. A structural pass never substitutes for independent semantic
review of all ownership groups.

Ordinary clean-checkout integrated reproduction remains incomplete while the referenced runtime
migration is unpublished. Do not supply archived source reconstructions as a supported fallback.
The standalone tool build/oracles and the working-tree integrated audit are distinct claims.
