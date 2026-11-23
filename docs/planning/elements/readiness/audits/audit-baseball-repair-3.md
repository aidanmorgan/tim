# Planning audit 3: `spec-repair-baseball-el-239.md` (Baseball repair, EL-239)

Audited 23 November 2026, independently of the first audit (`.tools/audit/audit-repair-baseball.md`). Phase: planning. Nothing was built or run. Contract read in the `spec-drafts` checkout (uncommitted, 4,455 words, baseline `25df3cd5`). Code read only with `git show 25df3cd5:<path>`. Binding decisions read from `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 0–24g, coordinator 16, 21, 25–36). Policy read from this checkout.

## Verdict: Incomplete

The repair's values, geometry, probe and role rows are right. The first audit's B1–B6 and N1–N8 are applied, and R9 is a sound addition. Five blocking defects remain, all text fixes; no redesign and no owner decision is needed:

- one defect in the read-only intent: the Always rule does not cover R9;
- one wrong specified value: R5's plate-top height is 80 mm off, so the planned resting assertion fails on a correct product;
- one task R9's test cannot execute as written without a shared-helper change the Never list forbids;
- the tier is wrong under the protocol's literal rule and the published slice's precedent;
- an unnamed prerequisite: the planning work this contract cites is not on main.

With B1–B5 applied it is a Scoped Pass (ready-for-dev), and the fixes need no new audit of the other rows.

## What was checked and holds

- **Code citations at `25df3cd5`.** All of these were opened and match:
  - `BaseballElementDeclaration.cs:11`, `:13`, `:22`–`:23`;
  - `baseball.tscn:13`, `:18`–`:23`, `:26`, `:35`–`:47`;
  - `VisibleSolidColliderTests.cs:63`, with the `ball` row at `:109` and `bowling` at `:110`;
  - `SceneGeometryExtractor.cs:24`, `MassFrameValues.cs:41`, `ColliderDescriptor.cs:21`;
  - `ConicalAirflow.cs:19`–`:26`, `WorkshopPhysicsEngine.Airflow.cs:126`, `:141`–`:162`;
  - `ContactLoadSensorDeclaration.cs:27` (inclusive: `mass < MinimumMass` is Underweight);
  - `Chambers.cs:131`, `CannonElementDeclaration.cs:31`, `WorkshopSaveCodec.cs:48`;
  - `NamedPackageSources.cs:13`, `:77`–`:79`, `CurrentCatalogue.cs:42`;
  - `CurrentCatalogueTests.cs:262`, `CurrentCapabilityInputsTests.cs:233`;
  - `TennisInteractionTests.cs:32`–`:47`, `TennisBallElementDeclaration.cs:16`, `CuriousContraptions.slnx:20`, `CuriousContraptions.tests.csproj:77`;
  - `docs/planning/elements/README.md:21`–`:31` and `:406`, `named-elements.md:2107`, `general-engine-element-map.md:349`, `scope-corrections.md:25`.
- **Values.**
  - Inertia: 0.4 · 0.15 · 0.26² = 0.004056.
  - Band: r − 0.018 = 0.242 and r + 0.01 = 0.27, with the Basketball's quaternion (`elements/Ball/ball.tscn:15`–`:18`, `:31`, `:42`).
  - `load_steps` stays 6: five sub-resources before and after.
  - Collider 0 stays the sphere because `col_visual_band` follows `col_sphere`. The airflow receiver names collider 0 (`BaseballElementDeclaration.cs:28`).
- **R9 (new).**
  - The default `capture_dwell` is 0.35 s (`ReceiverElementDeclaration.cs:46`), so the first capture comes at tick 42 or later at 120 Hz. This is the same rule as `WorkshopPhysicsEngineTests.cs:96`.
  - The residence sensor spans y −0.4 to +0.45 around the Receiver origin (`:12`–`:13`). The floor top is at −0.375 m (`basket.tscn:78`, height 0.15) and the inner walls are at ±0.69 m (`:84`, `:90`, thickness 0.12). A 0.26 m ball with its centre at +0.1 m starts inside the sensor, clear of every wall, and rests inside it.
  - The occurrence count read and its at-most-1 assertion are the existing construction's (`WorkshopPhysicsEngineTests.cs:89`–`:94`).
  - The source is real: `readiness/new-identities.md:123` (BB4) asks for the row, and main's EL-239 spec says "It remains a capture payload" (`EL-239-baseball.md:6`). No existing test captures a `baseball`: `CatalogueIntegrationTests.cs:201` covers `ball` and `bowling` only.
- **Test delta +11.** −1 for `MatchedSizeImpactChangesPayloadRouteByMass`, a `[Fact]` at `BaseballTests.cs:63`. Then +1 (R4), +6 (R5), +2 (R6), +1 (`Specs` row; the theory runs over `Specs.Keys`) and +2 (R9).
- **Coverage binding.** The binding section and `**Decision:**` form are unchanged. The task hash covers the spec and the S636 block (`NamedPackageSources.cs:77`). `git grep -i baseball 25df3cd5` finds no committed file that stores the old values or an enriched hash, and `docs/coverage/catalogue-elements.json:11653` and `engine/task-022.json:1548` hold only the requirement title. No level places a `baseball`.
- **Part B2.**
  - B2's contract (`spec-elements-visible-solid-remainder-b2.md:61`, `:68`, `:208`) states the same either-order rule:
    - if the repair publishes first, B2's `BaseballTests` edit is void;
    - if B2 publishes first, the repair re-reads its default-Tennis comparisons against the seamed ball.
  - The two contracts agree.
- **EL-223.** It is paused by owner decision 24g. The contract keeps main's binding form and leaves the `**Ownership:**` form to EL-223, with DC-85 noted. The first audit's B4 is resolved.
- **Staged-source rule.**
  - The slice adds no C# file, so `tools/anvil/CheckStagedTests.cs` (`:35`, `:41`–`:43`) has nothing to refuse.
  - The contract and the run log are Markdown.
  - The pre-commit snapshot (`.githooks/pre-commit:23`) sees every owned path once it is staged.
- **Forward-only.** The seam nodes, the old values and the withdrawn role test are deleted. The generic momentum law stays covered by `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`.
- **Element-free.** No engine, abstraction, tool or TypeScript path is in the owned scope. The scope guard at Verification `:204` checks it.
- **Budget and size.** 4,455 words and nine matrix rows: one coherent slice.

## Blocking findings

### B1. Intent: the Always rule names motion rows R3 to R8, so R9 is outside it (wrong specified behaviour)

- Where: contract `:31`.
- Defect: R9, a simulated capture, was added to the matrix (`:54`), but the Always line was not widened. The line is inside `<intent-contract>` and becomes read-only.
- Consequence: a reviewer cannot hold R9's test to the intent's "real production code" rule. A later fix would have to edit read-only intent.
- Fix: in `:31`, replace "the C# simulation for motion rows (R3 to R8)" with "the C# simulation for motion and capture rows (R3 to R9)". Optionally, in Approach (`:24`), after "add the lightest-ball rows" add ", the Receiver-capture row".

### B2. R5: "plate top at bench + 0.22 m" is the plate's origin; its top face is 0.08 m higher (wrong specified behaviour)

- Where: contract `:218`, Test column.
- Evidence:
  - `TennisInteractionTests.cs:32`–`:35` places the plate's origin at `WorkbenchSurface + .22f` and releases balls at origin + 0.34.
  - `elements/PressurePlate/pressure_plate.tscn:107`–`:108` puts the top box `col_0_1` at y 0.03, and its height is 0.1 (`:43`). The top face is therefore at origin + 0.08 = bench + 0.30.
  - A resting Baseball's centre is at bench + 0.56. The contract's "plate top + 0.26 m" read literally gives bench + 0.48, 80 mm low, against a 1 mm tolerance.
- Consequence: the resting assertion on every "off" case fails on a correct product. Alternatively, the implementer fits the number afterwards, which the testing guidance forbids.
- Fix: in row R5 replace "plate top at bench + 0.22 m, balls released 0.34 m above it" with "plate origin at bench + 0.22 m (its top face at origin + 0.08 m: `pressure_plate.tscn` `col_0_1` at y 0.03, height 0.1), balls released with their centres 0.34 m above the plate origin, that is resting on the top face". Replace "within 1 mm of plate top + 0.26 m" with "within 1 mm of bench + 0.56 m (top face + 0.26 m)".
  - Checked: at the centre the ball's 0.26 m sphere and the band's 0.27 m reach stay inside the ring's 0.282 m inner radius (`:36`, `:139`–`:141`), so the centre placement rests on the flat top. The ±0.5 m placements clear the ring and the studs at (±1, ±1).

### B3. R9's test is not executable as written without a forbidden helper change (test)

- Where: contract `:174`, `:222`.
- Defect: the cited construction uses `Free` and `AdmitMoving`, which are `private static` in `WorkshopPhysicsEngineTests` (`WorkshopPhysicsEngineTests.cs:226`, `:469`). Only `StepWorld` is `internal` (`:519`). The contract says the test is "built in the test with the part id `baseball`" but not how. Making either helper accessible is a shared test-helper change, which the Never list (`:35`) and the data-only tier exclude.
- Consequence: the implementer either edits a shared helper, which is a Never breach and a tier change, or invents an undescribed construction.
- Fix: in row R9, after "built in the test with the part id `baseball`", add: "The admission is made inline from public production calls, `PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2)), new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)))`, and stepped with `WorkshopPhysicsEngineTests.StepWorld` (internal, `:519`). `Free` and `AdmitMoving` (private, `:226`, `:469`) are not made accessible and not edited."
  - An equally acceptable alternative uses only public surfaces: `WorkshopIntegrationHarness` with a `SimulationEventKind.Captured` record whose `Other` is the Baseball, as `CatalogueIntegrationTests.cs:199`–`:231` does. Whichever is chosen, name it in the row.

### B4. The tier is data-only, but the owned diff includes `TODO.md`, which is outside the allow-list (wrong specified behaviour)

- Where: contract `:96`, `:118`, `:176`.
- Defect: the protocol says "a path outside the list makes the slice engine tier" (`bmad-loop-protocol.md:54`–`:62`). `TODO.md` is at the repository root, outside the literal list. The contract notes this but leaves it to the coordinator. The published Baseball slice set the precedent: it ran at engine tier "because new project build configuration and TODO changes are outside the data-only allow-list" (`spec-named-el-239.md:50` at `fa86969f`). Under "Mis-tiered slice" (`bmad-loop-protocol.md:75`), a slice found mis-tiered becomes engine tier from that moment: the current pass cannot approve, and all six layers run on the candidate as a first pass.
- Consequence: the first review launch is mis-tiered and its data-only pass is wasted.
- Fix: replace the first sentence of "### Review tier" (`:96`) with "**Engine tier (all six layers on the first pass).** Every capability is present with a symbol and an exercising test, and every product path is on the data-only allow-list, but the candidate commit carries `TODO.md`, which is outside it (`bmad-loop-protocol.md:62`; the published slice ran at engine tier for the same reason, `spec-named-el-239.md:50` at `fa86969f`)." Delete the sentence beginning "Two things for the coordinator's tier check". The alternative, dropping `TODO.md` from the owned diff, conflicts with "one candidate commit" (`bmad-loop-protocol.md:259`), so it is not recommended.

### B5. The slice's planning sources are not on main; the planning publication is an unnamed prerequisite (wrong specified behaviour)

- Where: contract `:62`, `:115`, `:147`, `:155`, `:176`, `:192`.
- Evidence: the work starts from a clean checkout of published main. Against `25df3cd5`:
  - `docs/planning/elements/readiness/` is untracked: it is not in `git ls-files` and not in `git ls-tree 25df3cd5`. It holds `owner-decisions-2026-11-22.md` (decisions 22, 24g and coordinator 36, which this contract relies on) and `new-identities.md` (BB4, decision 10's source).
  - `delivery-batches.md` on main has no "Repairs" table and no "Baseball repair" row (main section 5 is "Structural stories", `:223`). The row task 6 edits exists only in the uncommitted planning work (`delivery-batches.md:250` there).
  - `docs/planning/elements/README.md`'s Baseball row is `:406` on main and `:411`, with different text, in the planning work. `named-elements.md` and `sprint-status.yaml` are also modified there.
- Consequence: task 6 cannot execute against main. The decision citations do not resolve in the implementation checkout. Whichever of this slice and the planning work publishes second meets conflicts in `README.md` and `delivery-batches.md`.
- Fix: add a first bullet under "Serialisation" (`:155`): "Prerequisite: the planning work now uncommitted in `spec-drafts` (the `docs/planning/elements/readiness/` folder with the owner decisions of 22–23 November 2026, `delivery-batches.md` sections 0 and 5 with the Repairs table, `docs/planning/elements/README.md`, `named-elements.md`, `sprint-status.yaml`, `requirements.md`) is published before this slice starts, and the baseline is the main that contains it. At start the planner re-reads every planning `path:line` in this contract on that baseline (the README Baseball row moves from `:406` to about `:411`, with new status text that task 6 replaces)." In Code Map `:60`, replace "Baseline: published main `25df3cd5`" with "Baseline: the published main that contains the planning work (drafted on `25df3cd5`)".

## Non-blocking findings (apply with the above)

- **N1. Neighbouring repairs change two constructions this slice reuses** (document). Contract `:155`–`:159`.
  - Part A edits the Fan R4 uses (`spec-elements-visible-solid-remainder.md:70`–`:71`, `:140`–`:141`: solid Base, Post and Ring proxies declared as internal occluders).
  - Part C adds hidden proxies to the Cannon R6 uses (`spec-elements-visible-solid-remainder-c.md:64`, `:166`) and changes the `VisibleSolidColliderTests` matcher that R2's row runs through (`:67`).
  - Add to "Parts A, B and C share…": "Part A changes the Fan of R4 and part C the Cannon of R6 and the visible-solid matcher of R2. Whichever publishes later re-reads those rows' margins on the merged main; the thresholds are not refitted."
- **N2. S3, S5 and S6 are not named as sharing files** (document). They also run before the programme (owner decision 22) and edit `TODO.md`, `deferred-work.md` and `delivery-batches.md`. S3 also derives the lists that the contract's "Tests read and unchanged" cites (`PackageRuntimeParityTests.cs:149`, `:653`, `BasketballResourceTests.cs:34`, `WorkshopIntegrationHarnessTests.cs:694`, `CSharpStaticGateTests.cs:701`). Add: "S3, S5 and S6 share `TODO.md`, `deferred-work.md` and `delivery-batches.md`; one publisher at a time. If S3 publishes first, the membership lines cited above no longer exist; nothing here depends on them."
- **N3. The registration-check delta is not stated** (test). `:154` says the check is "re-measured the same way". The filter selects none of the changed classes (no `BaseballTests`, `VisibleSolidColliderTests`), so state "registration check: baseline + 0".
- **N4. Toolchain** (environment; the same note in all four audits). On this host the default `PATH` has Homebrew node v26.13.1 and Homebrew dotnet 10.0.403, which has no `wasm-tools`. The pinned toolchain is `~/.local/share/tim-toolchain/dotnet-10.0.403` (with `wasm-tools`) and `node-v26.12.1-darwin-arm64`. The capture's `test "$(node --version)" = v26.12.1` (`docs/delivery-workflow.md:42`) and the web build fail otherwise, and neither this contract nor the workflow says so. Add to Verification: "Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`; the session environment check is recorded in the run log."
- **N5. `VisibleSolidColliderTests.cs:110` insertion point** (document). `:110` is the `bowling` row. Say "after the `ball` row (`:109`)".

## Decisions

- Contract decisions 1–10 are defensible from the game's intent, and none cuts scope or waives a gate:
  - colours from the approved palette (`DESIGN.md:80`);
  - band geometry copied from the delivered Basketball;
  - no save migration, because a save stores the part id only;
  - R9 added from BB4.
- The target values match coordinator decision 36 (0.15 kg, radius 0.26 m, one proud band). The order matches owner decisions 22 and 24g.
- None needs the owner. For the coordinator: B5's publication order of the planning work, and B4's tier.

## Evidence limits

- Nothing was built or run. The R4, R5, R6 and R9 expectations are derived from the code and the declared values. The R4 ratios and band clearance are the first audit's derivation, re-read and not re-measured.
- I did not trace how a capture ends after the dwell when a ball rebounds inside the Receiver. The existing Basketball construction captures within 240 ticks at dwell 0.5 s, and the Baseball has the same bounce and is lighter.
- Parts A, B2 and C were read only for the files and constructions they share with this slice.
- EL-223's other checkout was not read; it is paused.
