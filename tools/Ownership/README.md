# Ownership inspection (P0-004 Design)

Build with `dotnet build tools/Ownership/Ownership.csproj -c Release`.
Run `dotnet tools/Ownership/bin/Release/net10.0/Ownership.dll --oracles` for the typed positive
and rejection cases. This executable uses the pinned SDK's Roslyn assemblies, without a new package.

`--audit . docs/work-orders/P0-004/ownership/index.json` rebuilds the real production/test
projects in Release and PLAYTEST contexts, plus the actual Animation Release project, captures actual generated binding sources under this
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

Read [the contract](../../docs/engine-contracts.md#ownership-and-assemblies) and
[analysis bounds](../../docs/engine-contracts.md#ownership-and-assemblies) before interpreting the results.
One assignment is required per authored member. The full current writer/caller context is
conservative, not a points-to proof. A structural pass never substitutes for independent semantic
review of all ownership groups.

Ordinary clean-checkout integrated reproduction remains incomplete while the referenced runtime
migration is unpublished. Do not supply archived source reconstructions as a supported fallback.
The standalone tool build/oracles and the working-tree integrated audit are distinct claims.

The portable Animation context retains the actual six linked sources, generated sources, compiler settings, references, project/assets/NuGet/import configuration identities. Its source-backed assembly replaces only an existing matching reference in actual game/test contexts; missing or duplicate required references reject. The retired Geometry project/context is removed; its former numeric context value rejects. Animation declarations and calls enter the census once. Its six current authored sources have no conditional-compilation branches; new branches/configurations require a fresh context-union review. Separate Release/PLAYTEST game/test contexts remain intact.

The existing oracles also exercise the same Compile/Inspect path with distinct source-backed assembly fixtures: member/use/caller identities, configuration separation, missing/duplicate/unknown contexts and inputs, binding errors, stale fingerprints and wrong ownership. These supplemental fixtures do not establish actual integrated capture or worker/browser qualification. Simulation-worker, JS and WGSL collector omissions remain explicit.

The earlier P0-022 integrated capture/native handoff was blocked by retained MSBuild child-node IPC failures. Normal build permissions now work: [P0-025's original integrated capture](../../docs/verification/P0-025-map/ownership-capture-1.json.gz) and its [affected current-source refresh](../../docs/verification/P0-025-map/ownership-capture-2.json.gz) have zero binding diagnostics across six then-current real compiler contexts, including portable Animation. The current normal assignment audit still rejects with `Source membership differs`; capture success does not establish assignment freshness, independent applicability or runtime qualification. No framework negotiation, isolation or archived-input bypass was used.

The audit reads declared owner membership from the current INVEST engine, GPU physics, refinements, decisions, scope corrections, current consumers and campaign documents. Explicit owner headings, P0 disposition cells and fixture/proof columns define membership; prose mentions do not. Missing or malformed authority input exits 1 with an ordinary boundary error. Repeated declarations coalesce; no suffix aliases, source-owner assignments or proof upgrades are inferred. Historical contracts still fail when their sources, assignments or owners are stale. TODO.md remains the current-work index; the deleted register is no longer an input.

Current semantic guards cover AnimationBatch slot/free-list storage, WorkshopSimulation operation identity and MachineWorld presenter collections. The free-list guard rejects unreviewed callers and reference recipients. `--oracles .` additionally captures the actual compiler contexts, verifies those five exact members and rejects wrong owners; it does not refresh assignments or qualify the whole ownership inventory. Retired uncompiled member guards are removed rather than retained as current examples.
