# Epic 7 legacy purge inventory (9 Oct 2026)

This inventory was taken read-only while Story 6.1d was uncommitted. Counts are given as on disk / tracked. Re-verify every count at the start of each story.

**Superseding archive-location decision (10 Oct 2026):** the owner requires “keep everything inside the tim directory”. Story 7.1 retains the verified archive, manifest and recovery under ignored `archives/story-7-1-20261009/`, excluded from game import/export. The original external-location decision below is historical; all recovery and preservation checks still apply.

## Owner decisions (9 Oct 2026)

1. **Order:** Epic 7 runs after Story 6.1d and before Story 6.2.
2. **Untracked reference bundles:** archive, then delete.
   - Tar `reference/p025-production-isolated-20261004/{diagnostic,production}-appbundle/` (2,474 untracked files) into a dated archive outside the repository.
   - Record the archive path and its sha256, then delete all of `reference/`.
3. **Story 7.4 is a full purge, specs first.**
   - Write a declaration spec in `docs/planning/invest/named-elements.md` for each of the 43 uncompiled part scripts.
   - Only then delete the scripts, the scenes that reference them, and their catalogue entries.
   - Elements are never removed from the plan. Each one remains a declaration spec to be rebuilt in its own element slice.
4. **Tools:** delete `tools/Campaign` and `tools/Playtest`. Campaign authoring is rebuilt on the current schema in Epic 15.

## 7.1 LEGACY-0a: `reference/`

On disk / tracked:

| Folder | Files |
|---|---|
| p025-production-isolated-20261004 | 4,324 / 218 |
| pipe | 100 / 68 |
| p054-guide | 38 / 6 |
| p054-contact-replay | 35 / 3 |
| switch-lamp | 35 / 3 |
| wall | 35 / 3 |
| P0-022-before | 33 / 24 |
| P0-025-before | 32 / 22 |
| CAT-001-I-r1 | 13 / 8 |
| P0-022-candidate-r1 / r2 | 9 / 9, 7 / 7 |
| cpu | 6 / 6 |

Also present are p020-clean-worker-assets, the `p0xx-pages-*` folders, four `P0-025-*.tar.gz` files, `README.md` and `.gdignore`.

- **Untracked:** the two p025 app bundles listed in decision 2.
- **Ignored:** 1,816 files, from `p025.../src`, `primitive-current` and the `bin/` and `obj/` folders.
- **Markdown:** 13 tracked `.md` files. No current document links to any of them, so none counts as active.
- **References from active files:**
  - Nothing in code, tests, any csproj, scripts or CI points at `reference/`.
  - `export_presets.cfg` lines 11, 39, 80 and 116 exclude `reference/*`. Tidy these.
  - The CAT-048b row in `docs/planning/invest/vertical-delivery.md` lists `reference/pipe/` as legacy it will delete. Update that row when 7.1 deletes it.

## 7.2 LEGACY-0b: `tools/` and `diagnostics/`

**Keep:**

| Tool | Why it stays |
|---|---|
| `tools/anvil` | Used by the pre-commit hook. |
| `tools/e2e` | The Chrome suites. |
| `tools/workshop-*.test.mjs` | The Node physics harness. |
| `tools/Preview` | Serves :8060. |
| `tools/LifecycleContract` | Current contract tool. |
| `tools/WireContract` | Current contract tool. |
| `tools/TraceAllocations` | Current analysis tool. |
| `tools/Coverage` | Must drop its hard-coded `engine/physics` paths (`CapabilityImplementationRequirements.cs:20-37`) in 7.4. |
| `tools/Ownership` | Must drop its `CuriousContraptions.Physics` names (`OwnershipPolicy.cs:39,103`) in 7.4. |

**Delete:**
- every `tools/p0-*` folder, including the empty `p0-002-review`;
- every `tools/P0-007-*` folder and `p0-007-probe`;
- `tools/PortableGeometryProof` and `tools/LifecycleContractReview`;
- `tools/GpuBodyFixture`: corrected on 10 Oct 2026 after direct inspection; this is a retired shader-f16 solver/readback experiment, not a current WASM SIMD fixture. Five tracked files and generated outputs belong to Story 7.2. Its shared WGSL files remain at the existing later migration/deletion gate;
- `tools/Performance` and `tools/Performance.Tests`;
- `tools/Playtest` and `tools/Campaign`;
- `diagnostics/`, which is compiled only by Performance and the P0-007 probes;
- `CuriousContraptions.Geometry`, which is used only by the P0-007 tools and PortableGeometryProof.

Update the tool list in `docs/README.md:41` to match.

## 7.3 LEGACY-0c: `CuriousContraptions.tests/`

**Execution receipt (10 Oct 2026):** Story 7.3 removed the 417 ledger paths below after exact compile-membership review; 26 local C# test/fixture sources remain, all compiled. The 36 evaluated items (including linked/package sources) are unchanged, and 643 solution tests pass. Original inventory facts follow as historical deletion evidence.

- **Uncompiled files:** the csproj compiles 26 of the 442 `.cs` files, plus two linked Simulation files, which leaves 416 uncompiled. No compiled test depends on any of them.
- **Junk:** `WoundSpringTests.cs.orig` is tracked and should go.
- **Shared data:** there are no fixture folders. The only shared data is `content/puzzles.json`.
- **Order:** `tools/Performance.Tests` compiles the uncompiled `PerformanceAuditTests.cs`, so 7.2 must land before 7.3.

## 7.4 LEGACY-0d

- **`engine/physics/`:** 134 `.cs` files. No slnx project compiles them. They become deletable once 7.2 removes the probes.
- **Other dead engine code:** `engine/bridge/` (13 files, all uncompiled) and 71 uncompiled `engine/*.cs` files, such as the `Scene*Declaration` files, the networks, `Simulation*` and `MachineData`.
- **`engine/presentation/`:** all six files are compiled and used. Delete nothing there.
- **`parts/`:** 9 of 52 scripts are compiled: Ball, Basket, Ramp, Switch, Lamp, Wall, Delay, Bumper and Domino. The other 43 are referenced by `parts/scenes/*.tscn` (69 scenes) and by catalogue `.tres` files, including `engine/PipeDimensionsResource.cs`.
- **Pipe:** `engine/gpu/WorkshopPipe.cs`, `AnnularProfile.cs`, `PipePart.cs` and `PipeTests.cs` are parked CAT-048 work. TODO.md says to preserve Pipe, so decide its handling explicitly in the 7.4 spec.
- **Other documents to update:** `docs/planning/invest/current-consumers.md` has 72 references to `parts/*Part.cs` or `engine/physics`.
- **Audits:** P0-030 and P0-031 close here (`docs/planning/invest/engine.md:31`). `docs/work-orders/P0-003/workloads.md:68` links a dead `TODO.md#work-p0-031` anchor.

## Cross-story order

1. 7.1 can go at any point.
2. 7.2 comes before 7.3 and before 7.4.
3. In 7.4, the declaration specs come before any deletion of scripts, scenes or catalogue entries.
