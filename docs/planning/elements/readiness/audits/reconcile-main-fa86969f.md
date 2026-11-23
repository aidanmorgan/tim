# Reconciliation of planning drafts with published main `fa86969f` (Baseball, EL-239)

Written 21 November 2026. Read-only audit. Worktree `spec-drafts` is at `8d1d68aa` with uncommitted drafts; main was read with `git show origin/main:<path>` and `git diff 8d1d68aa origin/main`. "Main" below means `origin/main` at `fa86969f`; a `path:line` prefixed "main" is a line in that revision, otherwise it is a line in this worktree. Nothing was built or run.

## Decisions needed, most important first

1. **The delivered Baseball contradicts the owner's 21 November decisions.** It is 0.65 kg (decision: about 0.15 kg, lighter than the Soccer ball, "the lightest ball"), and it has two proud seams with no collider and no proud band with a collider. Either a repair slice changes the published package, or the owner accepts the delivered values and the planning decisions are withdrawn.
2. **Two coverage bindings now exist on paper.** Main binds through a `## Package source binding` section in the element spec; 46 drafted element specs and the element-spec brief say a `**Package:**` ledger line (drafted S7). Main's rule can bind only 4 of the 318 ledger identities as the documents stand, and not the Soccer ball or the Cannonball. One rule has to be chosen and generalised.
3. **Another session is already changing that binding** (EL-223 draft: `**Decision:**` becomes `**Ownership:**` with kinds Decision and Slice). S7 planning must be reconciled with that draft, not only with main.
4. **The Baseball is outside `VisibleSolidColliderTests`**, so the full round passed without checking its art against its collider.
5. **The untracked `docs/planning/elements/EL-239-baseball.md` in this worktree collides with the tracked file on main**; merging main here fails until the draft is moved or removed.

## 1. How main binds a named-identity package

### The rule

- A binding is a section in the element spec, not a ledger line. `NamedPackageSources.Read` scans `docs/planning/elements/EL-*.md` and keeps the files containing `## Package source binding` (main `tools/Coverage/NamedPackageSources.cs:13`, `:19`–`:23`). A spec without the section is planning prose.
- The section must be exactly five lines after the heading (regex at main `NamedPackageSources.cs:55`–`:61`):
  - `**Package:** <id>.` with id `[a-z0-9_]+`, no backticks;
  - `**Named identity:** [EL-NNN](../invest/named-elements.md#element-NNN).` (three digits, `EL` only);
  - `**Acceptance:** [todo-N](../requirements.md#todo-N), [sequence-task-M](../requirements.md#sequence-task-M).`;
  - `**Decision:** [SNNN](../invest/scope-corrections.md#sNNN).`;
  - `**Mode records:** <modes>.` parsed by `CurrentCatalogue.ParseModes` (main `tools/Coverage/CurrentCatalogue.cs:159`–`:165`), exactly one `Configuration` mode.
- Checks: the file name starts `EL-NNN-` and the title starts `# EL-NNN · ` (main `NamedPackageSources.cs:64`–`:66`); the ledger entry has that heading, exactly one `../requirements.md#todo-N` link and it is the bound one (`:93`–`:99`); the ledger entry has exactly one `Owner **SNNN**` and it equals the bound decision (`:101`–`:108`); the decision anchor exists once in `scope-corrections.md` with a non-empty body (`:121`–`:143`); the todo and sequence-task anchors are adjacent in one requirements row (`:110`–`:119`); the task is a discovered task source (`:76`).
- Uniqueness: one package, one identity and one task source per binding (`:41`–`:49`), and a package id may not be both a catalogue key and a named package (main `CurrentCatalogue.cs:237`–`:239`).
- Admission: `ValidateResources` admits a declaration whose id is a catalogue key or a named package; any other is still refused with "has no current catalogue declaration" (main `CurrentCatalogue.cs:251`–`:252`). `Read` attaches the bindings at `:42`.
- Source identity: no new source is created. The existing task source (`sequence-task-530`) has its hash replaced by a hash over the task, the spec file, the ledger entry and the decision block (main `NamedPackageSources.cs:77`–`:79`; `tools/Coverage/RequirementDiscovery.cs:23`–`:25`).
- Capability inputs: `AttachNamedPackages` adds the spec, the package and its artwork closure to the task source's required artifacts and symbols, and adds the mode contract only when the task already has a proof owner (main `tools/Coverage/CapabilityInputs.cs:137`–`:157`). The task keeps its task classification (`:203`–`:216`); it is not classified as a current part.
- Every refusal is an `InvalidDataException` with a fixed message that does not name the path, identity or package (for example "Malformed named package source binding.", main `NamedPackageSources.cs:62`).

### The parsed text and the tests

- Parsed text: main `docs/planning/elements/EL-239-baseball.md:14`–`:20`.
- `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` (positive, an unfamiliar `EL-987` control, and the real repository); `NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings` (17 rows); `NamedPackageUniquenessIsDiscriminatingForEachKey` (4); `NamedDecisionMustMatchExactlyOneLedgerOwner` (4); `CurrentNamedDecisionCannotDetachItsAcceptanceAndFreshness`; `NamedAcceptanceChangesInvalidateOnlyItsTaskProof` (7) — main `tools/Coverage.Tests/CurrentCatalogueTests.cs`, added block.
- `CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion` (copied repository: missing proof owner stays `MissingScope`, artwork and symbols required, scene or declaration removal refused).
- Not updated on main: `tools/Coverage/README.md` and `docs/delivery-workflow.md:62`–`:65` do not mention the binding, and the documented registration filter still omits `CurrentCapabilityInputsTests`, although the slice contract says the registration command adds it (main `_bmad-output/implementation-artifacts/spec-named-el-239.md:96`). `docs/planning/elements/README.md` and the ledger header do not describe the section either.

### Difference from drafted S7

| Point | Main | Drafted S7 (`spec-s7-coverage-named-identity-admission.md`) |
|---|---|---|
| Where the binding lives | Section in the element spec | One ``**Package:** `<id>` `` line in the ledger entry (`:23`, `:47`–`:48`) |
| Identity kinds | `EL-NNN` only | element, thermal, radiation, gap (`:47`) |
| Preconditions | todo row co-anchored with a sequence task, one `Owner **SNNN**`, an existing decision block | A ledger identity line only (`:54`, N8) |
| Source record | Enriches the existing task source | None created (decision 5, `:140`) |
| Modes | `Mode records` required | None until a later story (`:136`) |
| Artwork closure | Through `Prepare` for each named package | A new assertion over every admitted path (task 4, `:165`) |
| Refusal messages | Fixed text, no names | Name the path, identity and package (`:29`) |
| Symbols | `NamedPackageSources`, `CurrentCatalogue.NamedPackages` | `NamedIdentityBindings`, `CurrentCatalogue.NamedIdentities` (`:162`–`:163`) |

### Is S7 superseded?

**Partly.** The mechanism S7 designed is replaced, but most of S7's purpose is not delivered.

- Delivered by main, so no longer needed from S7: admission of a non-CAT package (S7 capability 9, `:99`); a reader of planning documents for bindings (capability 8, in a different file); the artwork closure check for a bound package; uniqueness and catalogue-collision refusals (N3, N4, N5 in other words).
- **Gap A, coverage of identities (the main gap).** Measured on main's ledger, requirements and scope corrections with the parser's own conditions: 4 of 318 identities can be bound today (`element-232`, `-234`, `-238`, `-239`). Failing conditions, not exclusive: 285 have no decision block in `scope-corrections.md` (it has 14 `sNNN` anchors, main `scope-corrections.md:5`–`:102`); 149 have no todo link; 140 have a todo that is not co-anchored with a sequence task; 21 have no `Owner **SNNN**`; 77 are not `EL`. Soccer ball: owner S647 has no decision block and `todo-217` is a "Scope index" row (main `named-elements.md:718`–`:721`). Cannonball: owner S661, `todo-197`, the same two failures (`:844`–`:847`). EL-217, EL-223 and EL-241 say "no earlier S owner" (`:1997`, `:2027`, `:2117`), and EL-241 links a sequence task, not a todo.
- **Gap B, lesson fixtures (S8).** Unchanged on main: a fixture row must name a catalogue key and a `CAT-NNN-V` criterion (main `CurrentCatalogue.cs:116`–`:120`, `:194`; `RequirementDiscovery.cs:85`–`:86`). A level cannot place the Baseball as a fixture.
- **Gap C, capability classification.** The Baseball's task has no proof owner; main records this as deferred work (main `deferred-work.md:3`–`:5`) and the test asserts `MissingScope`. S7's exclusion (b) is still open in a different form.
- **Gap D, documentation and the registration filter.** See "Not updated on main" above.
- **Gap E, refusal messages** do not name the declaration, identity or package; S7 required that.
- **Gap F, one task per package.** Two packages cannot share one task source (main `NamedPackageSources.cs:47`–`:48`), which a Family slice under one requirement row would hit.

## 2. The delivered Baseball against the owner's decisions

### Declaration (main `elements/Baseball/BaseballElementDeclaration.cs`)

| Field | Delivered | Line |
|---|---|---|
| Id, category, name | `baseball`, Motion, "Baseball" | `:10` |
| Description | "A medium-light seamed ball with moderate bounce." | `:11` |
| Colour | (232, 220, 203) of 255 | `:11` |
| Palette order | 68 (Tennis 64, Balloon 65, Fan 66, Windmill 67) | `:12` |
| Pick radius | 0.35 m | `:13` |
| Material | restitution 0.35, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s | `:17` |
| Mass, inertia | 0.65 kg; 0.01625 kg·m² each axis (0.4 · 0.65 · 0.25²) | `:22`–`:23` |
| Ambient drag | 0.04 1/s | `:25` |
| Parameters, ports, events | none | `:12`, `:15`, `:18` |
| Payload, airflow | `CaptureTarget`; one receiver, four samples | `:21`, `:27`–`:30` |
| Collider | none declared; read from the scene: one `SphereShape3D` radius 0.25 m | `:16`; main `elements/Baseball/baseball.tscn:25`–`:26`, `:45`–`:47` |

### Scene nodes (main `elements/Baseball/baseball.tscn`)

- `Visual/Sphere`: `SphereMesh` radius 0.25 m (`:11`–`:16`, `:32`–`:33`).
- `Visual/SeamLeft`, `Visual/SeamRight`: one `TorusMesh` with `inner_radius` 0.213 and `outer_radius` 0.219 (`:18`–`:23`), at x = −0.125 and +0.125, turned 90° about Z (`:35`–`:43`).
- `col_sphere`: the only collision shape (`:45`–`:47`).

**The seams are proud visible solids without a collider.** Each ring has centreline radius 0.216 m and tube radius 0.003 m in the plane x = ±0.125 m. The ring centreline is √(0.125² + 0.216²) = 0.2496 m from the ball centre, so the tube reaches 0.2526 m: about 2.6 mm outside the 0.25 m collider, all round both rings. The spec states it: "The seams are cosmetic; the only collision shape is the 0.25 m sphere" (main `EL-239-baseball.md:8`). They are not printed flat, enclosed or moved by a binding, so under "visible = solid" they are a finding of the same kind as the Tennis ball's seam (`spec-engine-art-solidity.md:91`, "a torus 3 mm proud of the felt").

**`VisibleSolidColliderTests` does not see it.** The theory runs over `Specs.Keys` (main `CuriousContraptions.tests/VisibleSolidColliderTests.cs:63`, `:172`) and the file has no `baseball` entry (the slice did not change the file). `BaseballTests.SceneAndIconExposeSeamedSphereMatchingContactGeometry` asserts the seams are tori at |x| = 0.125 and the single collider is a 0.25 m sphere (main `CuriousContraptions.tests/BaseballTests.cs:124`–`:149`); it does not compare the seam extent with the collider.

### Tests (main `CuriousContraptions.tests/BaseballTests.cs`)

- `IdenticalDropDistinguishesEveryRetainedPreset` (`:25`–`:61`): first-rebound peak differs by at least 0.15 m from Basketball, Bowling ball and Tennis ball; Balloon rises. Recorded peaks: Baseball 0.166 m, Basketball 0.785 m, Bowling −0.119 m, Tennis 1.996 m (main `evidence-named-el-239/checks.txt:69`–`:73`).
- `MatchedSizeImpactChangesPayloadRouteByMass` (`:63`–`:81`): 0.65 kg striker sends a 0.5 kg target past 1.4 m; a 0.35 kg Tennis control stays short of 1.2 m.
- `UnpoweredDropSettlesWithoutFreeRebound` (`:102`–`:122`), `CommittedPresentationFollowsMovingBodyAndReset` (`:151`–`:189`), `FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` (`:191`–`:230`), `RunPauseStepResetSaveLoadRestoresAuthoredBaseball` (`:232`–`:258`).
- `FanGuidanceTests` gains two Baseball rows (main `CuriousContraptions.tests/FanGuidanceTests.cs:9`–`:26`).
- Not tested: pressure-plate load, cannon fit, fan response compared with other balls.

### Disagreements with the owner's decisions of 21 November 2026

| # | Owner decision (source in this worktree) | Delivered | Consequence |
|---|---|---|---|
| D1 | Realistic proportions, about 0.15 kg, a third of the Soccer ball (`spec-family-ball-presets.md:157`–`:158`; brief `:25`) | 0.65 kg | 4.3 times the decided mass; heavier than the planned 0.45 kg Soccer ball and the 0.35 kg default Tennis ball |
| D2 | Distinct role "the lightest ball": the Fan moves it most; two do not press a default 0.5 kg plate (`spec-family-ball-presets.md:160`) | Role "medium-light", proven by rebound height and by a heavier-striker route | One Baseball alone presses a default 0.5 kg plate; a default Tennis ball accelerates 1.9 times faster in a fan. The delivered mass test asserts the opposite direction (heavier than the light control) |
| D3 | Each ball preset has one proud band with its own collider (`spec-family-ball-presets.md:153`; brief `:25`) | Two thin seams, proud by 2.6 mm, no collider; no band | Breaks "visible = solid" and the banded-ball decision |
| D4 | Radius 0.26 m, between Tennis and Bowling (`spec-family-ball-presets.md:159`) | 0.25 m, equal to the Tennis ball | Silhouette is the Tennis ball's size |
| D5 | Rolling resistance 0.04 (proposed) | 0.03 | Minor; proposal only |
| D6 | Tan `#e8b764` sphere, coral `#ed6378` band | Cream (232, 220, 203), seam (217, 107, 88) | Not checked against `DESIGN.md` here; the approved-palette status of both delivered colours is unverified |
| D7 | Description "The lightest ball. A fan carries it easily and it lands with a low bounce." | "A medium-light seamed ball with moderate bounce." | Follows D1 |
| D8 | Delivered inside the ball-presets Family slice | Delivered as a single element | The Family slice now has two members |

Agreements: restitution 0.35, friction 0.3, drag 0.04, no parameters, palette order 68, package id `baseball`, directory `elements/Baseball/`.

Main's element spec also has none of the six sections that `docs/planning/elements/README.md:21` requires ("Every spec has six sections"); it is five paragraphs and the binding section.

## 3. What the slice changed in planning documents, and what is stale here

### Changes on main

- `docs/planning/invest/delivery-batches.md:17`: the Ball presets row links the Baseball spec, "native verification in progress"; `:91`: "Baseball now has an element spec, while the other 24 remain probe candidates". No S7 or S8 entry.
- `docs/planning/invest/named-elements.md:2107`: the EL-239 **Backlog** sentence is replaced by an **Implementation** sentence. No package line.
- `docs/planning/elements/README.md:147`: "now include the Baseball spec; the other 24 have no spec"; `:402`–`:406`: new section "Campaign addition: Baseball", status "implementation verification in progress".
- `docs/planning/invest/scope-corrections.md:25`: S636 result paragraph (0.65 kg, 15 cm separation).
- `_bmad-output/implementation-artifacts/deferred-work.md:3`–`:9`: two entries — `sequence-task-530` has no proof-owner role; EL-223, EL-241 and EL-217 composition probes go to the next cluster-2 planning owner.
- `TODO.md`: outcome is EL-239; full round 7,294/0/0, registration 798/0/0.
- `sprint-status.yaml:737`: `named-el-239: done`.
- Several of these still say "in progress" or "pending verification" although the slice is published (README `:406`, `delivery-batches.md:17`, `EL-239-baseball.md:12`).

### Per-package lists the Baseball slice edited by hand (evidence for S3)

| File on main | Edit |
|---|---|
| `CuriousContraptions.tests/CSharpStaticGateTests.cs:701` | project path added to the trim list |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:149` | `baseball` added to the payload name pin |
| `CuriousContraptions.tests/PackageRuntimeParityTests.cs:653` | `baseball` added to the generic placement switch |
| `CuriousContraptions.tests/BasketballResourceTests.cs:34` | `baseball` added to the literal registry list |
| `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs:694` | `baseball` added to the palette rows |
| `CuriousContraptions.slnx:20` | project line (S3 keeps this one by decision D1) |
| `CuriousContraptions.tests/CuriousContraptions.tests.csproj:183` | `Compile` line (S2) |
| `CuriousContraptions.tests/FanGuidanceTests.cs:9`–`:26` | payload enum and switch (a behaviour row, not membership) |

All four lists S3 derives or deletes were edited, as S3 predicts (`spec-s3-remaining-per-package-lists.md:52`–`:56`). `VisibleSolidColliderTests.Specs` was not edited, which is the S4 gap (`:90`).

### Stale drafts and the exact changes needed

**`spec-s7-coverage-named-identity-admission.md` — rewrite; do not implement as drafted.**
- Intent (`:21`–`:23`): the problem is no longer "no non-CAT package is admitted"; it is "the named binding admits only an identity with an S decision block and a co-anchored todo and task (4 of 318)". Remove the `**Package:**` ledger line as the approach unless the owner chooses it over main's section.
- Replace `NamedIdentityBindings`, `NamedIdentities` and the three-to-four-argument `ValidateResources` change (`:109`–`:112`, tasks 1–5) with changes to `NamedPackageSources` and `NamedPackages`.
- Rebase every `path:line`: the refusal is at main `CurrentCatalogue.cs:251`–`:252`, not `:242`–`:243`; "No tool reads the ledger" (`:82`) and "44 delivered packages are CAT parts" (`:174`) are false on main.
- Drop task 4's closure assertion or restate it: main already runs the closure for named packages.
- Keep and re-own: S8 lesson fixtures (task 9, `:180`–`:185`), the capability-classification exclusion (task 10), the documentation and registration-filter edits (tasks 6–8, reworded for the section), and named refusal messages.
- Add: the thermal, radiation and gap identities; identities with no S owner; identities whose source is an `element-NNN` anchor or a bare sequence task; two packages under one requirement row.
- Reconcile with the in-flight EL-223 draft (section 4) before any of this is planned; that slice edits the same files.
- Shared-files list (`:150`–`:155`): add the EL-223 slice.

**`spec-family-ball-presets.md` — replan as a two-member family or a Baseball repair plus a two-member family.**
- Title, Problem and Approach (`:2`, `:21`–`:23`): the Baseball package exists. Remove Baseball from "no package" and from the new-file lists (`:108`, `:113`–`:114`, task 1 column, tasks 6–7).
- Rows BB1–BB5 (`:57`–`:61`) and AC-239 (`:206`) describe a 0.15 kg, 0.26 m, banded Baseball that does not exist. Either move them to a named Baseball repair slice (mass, radius, band, description, tests, spec, S636 paragraph) or delete them if the owner accepts the delivered ball.
- Blocker (`:70`, `:98`): "blocked on S7" becomes "blocked on a binding that admits S647 and S661 identities": neither has a decision block, and `todo-217` and `todo-197` are scope-index rows.
- Task 8 (`:200`): replace the three `**Package:**` ledger lines with a `## Package source binding` section in `EL-077-soccer-ball.md` and `EL-091-cannonball.md`, in whatever syntax is current when the slice starts.
- Lists (`:117`–`:124`): rebase on main (payload pin becomes `ball, balloon, baseball, bowling, cannonball, soccer, tennis` from a list that already has `baseball`); `:133` "search returns nothing" is false.
- Palette order (`:179`): 66 and 67 are the Fan and the Windmill on main and 68 is the Baseball; Soccer and Cannonball need 69 and 70, and row F1 (`:64`) "after Balloon" needs rewording.
- `VisibleSolidColliderTests.Specs` (`:191`): the Baseball row cannot be `["Visual/Visual_Sphere"], ["Visual/Visual_Band"]`; the delivered nodes are `Visual/Sphere`, `Visual/SeamLeft`, `Visual/SeamRight`.
- "For the owner's information" (`:163`): EL-239 now has a spec.

**`spec-s3-remaining-per-package-lists.md` — still needed; rebase only.**
- "44" becomes 45 throughout (`:52`, `:55`–`:56`, `:77`, `:81`–`:85`, `:141`, `:156`); trim entries are main `CSharpStaticGateTests.cs:681`–`:704`.
- Baselines (`:142`, `:183`, `:187`–`:188`): 7,246 and 730 become 7,294 and 798 on main; the hook count must be re-measured.
- `:90`: "33 of 44" becomes 33 of 45.
- Add the table above as evidence that a slice published after registration derivation still made four list edits.

**`spec-early-level-proof-batch-1.md` — still valid; small rebase.**
- `:53`, `:61`: baseline `8d1d68aa` and "44 packages" become main and 45. The Baseball is in no level, so the level counts (`:63`–`:64`) are unchanged.
- `:35`, `:77`, `:165`: the comment is still at main `tools/Coverage.Tests/CurrentCatalogueTests.cs:61`, but the file gained about 285 lines; re-check any other line reference into it.
- Add to "Never" or the Code Map: no level in the batch may place `baseball` as a fixture (gap B).

**Element-spec brief (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/37b1f527-b6f7-4082-bfd2-686d62e814b3/scratchpad/element-spec-brief.md`).**
- `:37`: "the `**Package:**` ledger line for a named identity" is wrong on main. Replace with: "a `## Package source binding` section in the element spec, in the syntax current on published main; state whether the identity meets its preconditions (S decision block, co-anchored todo and task) and, if not, name the coverage story that must admit it".
- `:25`: "Baseball has realistic proportions (0.15 kg)" must note that the delivered package is 0.65 kg pending the owner's decision.
- `:7`: the checkout baseline is one slice behind main.
- `:21`: `spec-s7-…` is no longer a model.

**Element specs written under that brief.** 46 files under `docs/planning/elements/` in this worktree contain `**Package:**` (for example `EL-039-air-reservoir.md:18`, `:204`; `EL-220-docking-lift.md:101`; `EL-237-boxing-glove.md:99`). Each "Coverage binding" sentence and each "prerequisite S7" needs the same replacement. `EL-217-reload-shuttle.md:30` sets `minimum_mass` 0.1–1 kg because "the Baseball is 0.15 kg".

**`docs/planning/elements/EL-239-baseball.md` (untracked here).** Main tracks a different file at this path. The draft describes a 0.15 kg, 0.26 m, banded ball and a `**Package:**` binding, and says "Status: Not started". Move it out of the path before merging main; keep it as the proposed text of a Baseball repair if the owner confirms D1–D4.

**Other drafts touched by the count.** `spec-s4-catalogue-conformance-suite.md:92` ("33 of 44"), `spec-engine-art-solidity.md:52`, `:54`, `:75` ("all 44") and its findings table (`:85`–`:92`) need a `baseball` row: `Visual/Sphere` solid; `Visual/SeamLeft` and `Visual/SeamRight` proud by 2.6 mm with no collider. `spec-engine-goal-evaluation.md:135` ("44 packages").

## 4. Other sessions in flight

| Worktree | Branch, HEAD | State | Building |
|---|---|---|---|
| `/Users/aidan/orca/workspaces/tim/tim-cluster2-next` | `aidanmorgan/tim-cluster2-next`, `fa86969f` | clean | Nothing now. It built and published the Baseball |
| `/Users/aidan/orca/workspaces/tim/tim-cluster2-after-baseball` | `aidanmorgan/tim-cluster2-after-baseball`, `fa86969f` | `TODO.md` modified; untracked `spec-named-el-223.md` and its run log | **EL-223 Passive impact scoop**, draft contract, planning audit next, no implementation yet |
| `/Users/aidan/orca/workspaces/tim/tim-12-4-bellows` | `aidanmorgan/tim-12-4-bellows`, `4f193bb2` | staged `spec-12-4-pneumatic-bellows.md` (blocked) and `spec-engine-axial-gas-nozzle.md` (draft, planning Incomplete) | CAT-010 Bellows, blocked on gas, nozzle and prismatic engine slices; last written 16:49 |

Details of the EL-223 draft that planning must not contradict (`tim-cluster2-after-baseball/_bmad-output/implementation-artifacts/spec-named-el-223.md`):

- Package `passive_impact_scoop`, `elements/PassiveImpactScoop/` (`:72`); acceptance `todo-412` / `sequence-task-377` (`:24`).
- **It changes the binding again.** It classes the S-owner requirement as "narrower" (`:64`) and plans one model with `NamedOwnershipKind { Decision, Slice }`; the binding line `**Decision:**` becomes `**Ownership:** Decision [S636](…)` or `Slice [EL-223](…)`, with no reader of the old form, and the Baseball spec is rewritten in the same candidate (`:145`, `:147`; owned paths `:79`–`:82`). This closes the "no S owner" part of gap A for identities that have a co-anchored todo and task. It does not cover thermal, radiation or gap identities, identities with an S owner but no decision block (Soccer ball, Cannonball), or EL-241 ("different ledger acceptance syntax is not implemented prospectively here", `:147`).
- Routing it records (its `TODO.md:7`): EL-215 owns the later damped-cushion integration; EL-217 needs a launcher-return prerequisite routed to cluster 6 / REQ-12; EL-241 is the next untested composition probe for the cluster-2 planning owner.
- It does not implement S2–S6 (`:32`).

Planning in this worktree should therefore not draft slices for EL-223, and should treat EL-241 and EL-217 as claimed for probing by the cluster-2 owner. The drafted `EL-217-reload-shuttle.md` here overlaps that routing and assumes the 0.15 kg Baseball.

## Not verified

- Nothing was built or run; test results are those recorded on main.
- Whether the delivered Baseball colours are in the approved palette.
- The hook test count on main.
- Whether the EL-223 draft has changed since it was read (it is uncommitted and in planning).
