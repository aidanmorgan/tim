# Planning audit 4: ELEMENTS-VISIBLE-SOLID-REMAINDER part C (Cannon, Windmill, Balloon)

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md` (5,473 words). Checkout `spec-drafts`, HEAD `25df3cd5`, product and test paths identical to `25df3cd5`. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B, B2 and D.

## Verdict: Scoped Pass (sequence-gated)

No finding is inside `<intent-contract>`; the intent can freeze. By its own rule (lines 54, 233–235) the contract is re-read on the main that publishes part B2 before it is ready for development, because `Solid.Inner` and the low-restitution theory exist only after part B. That re-read is a precondition, not a defect: it can be limited to confirming part B's delivered names and the line anchors, with no new design review. Apply N1–N2 before dispatch.

## Earlier blocking findings (audit 3), resolved in substance

- F1 (string exception attributed to `ENGINE-CONNECTION-MATERIALS`): line 23 now says the string "keeps its named exception (owner, 21 November 2026; owner decision 1, 22 November 2026), which `ENGINE-ART-SOLIDITY` declares as the package's cited decorative entry", consistent with C9 and owner decision 1.
- R1 (what C8 settles): outcome (b) at line 152 states C8 settles the native legs only and the `first-committed-sample` entry adds a queued browser case.
- R2 (Baseball Cannon test): `BaseballTests.BaseballNeverSeatsInTheCannon` in C12 and `BaseballTests` in the filter.
- R3 (precondition): line 56.
- R4 (stale lines): applied.

## What was re-verified at `25df3cd5`

- `cannon.tscn`: imported `JacketArt`, `CollarArt`, `SleeveArt` (lines 3–5); `Jacket` 88, `Sleeve` 103, `ChargeTrack` 116 (0.8 × 0.11 × 0.07 at (0, −0.55, 0.62)), `Indicator` 128 ((−0.66, 0.65, 0.4), r 0.07), proxies 132–158 (hidden mesh style), `col_Foot` 155, `PowerIn` 163, `ActivationIn` 170. All twelve `Visual` meshes are classified by the new row. The four new proxies reuse existing meshes, so `load_steps` is unchanged.
- `windmill.tscn`: 19 resources, `load_steps=20` (21 after `Shape_Output`); `Mesh_Output` 61, `col_guard` 88 (hidden, 0.815 m), `Visual/Guard` 112, `Output/Wheel` 202 (r 0.24, h 0.12, axis Z, at (0.25, −0.1, 0.44)). `WindmillElementDeclaration.cs:38–39`: nine occluders today (base 0–3, rotor 0–4); samples at (−0.12, ±0.42, 0) and (−0.12, 0, ±0.42).
- C6 need: with Fans at (0, 5, 0) and (6, 5, 0) and the Windmill at (3, 5, 0), the rear stream's ray to the (−0.12, 0, 0.42) sample crosses the wheel (|x − 0.25| ≤ 0.218 at y offset 0.1, z 0.38–0.5).
- Balloon: `Envelope` is a unit sphere scaled by `CommittedGasRadius`/`UniformScale`, clamp from the dry radius to 16 (`BalloonElementDeclaration.cs:27–31`); radius parameter 0.18–0.5, default 0.36 (`:23`); `Carrier` r 0.0625 inside; `String` rod r 0.006, length 0.85; paths have no `Visual/` prefix, as the row says.
- Drops: C1 indicator apex 0.72, breech top 0.58, rear collar 0.495 m and jacket 0.564 m from the resting centre; C5 `Back` wheel face 0.5, nacelle stop 0.275, guard 0.436 m away. Static restitution 0 for both, so part B's stop-only theory applies.
- Matcher anchors: `Tol` 33, `Solid` 61, `Specs` 63–162, `Art` 242–314 with the parameter-only binding loop `:259–273` and shape switch `:279–294` (no `ArrayMesh` arm). G1 and G2 are test-harness code; neither names a package.
- Presentation citations: `WorkshopDeclaredPresentation.cs:402–405` (`OuterRadius` over the committed volume), `:637–649` (stopped projection holds its anchor), `MachinePart.cs:211–221`, `:262–267` and `ElementPresentation.cs:184–199` (construction and Reset draw `InitialValue`, 0.0625 for the envelope), `MachineWorld.Gpu.cs:251–252`, `:317`; `IsobaricGas.cs:130`; `.githooks/pre-commit:52–54` runs two scene-free test classes.
- Counts: rows 41 at start (42 with the Baseball repair) + 3; AC4 +10 (3 rows, 2 drops, 3 envelope cases, 2 facts); AC2 as stated. `content/puzzles.json` has no Cannon or Windmill and one Balloon (`burst_of_motion`).
- Every cited test exists (`CannonIntegrationTests` two, `CannonArtTests`, `WindmillAdmissionTests`, `RotaryAirflowRuntimeTests`, `WindmillIntegrationTests`, `BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion`, `BodyOwnershipTests`, `CompoundMassAndRollingTests`).
- Decisions: owner decision 5 applied as its conditional ((b) proceed with the named defect owned by `ENGINE-ONE-WAY-INSTALL`, order 6, `engine-stories-index.md:18`; (a) stop); Mechanical link schematic (owner 2, coordinators 25 and 36); string exception (owner 1); before the programme (owner 22); no wire or ABI change (24j/24k); presentation projection unchanged (Never clause).

## Non-blocking corrections (apply before dispatch, no re-audit)

### N1. Frontmatter baseline

- Where: line 6 `baseline_revision: '25df3cd5…'` against line 56 (baseline is the main containing the planning work and parts B and B2).
- Consequence: a checkout rooted at `25df3cd5` has no `Solid.Inner` or low-restitution theory, which tasks 2 and 5 extend.
- Fix: delete the line; the coordinator captures it at replanning.
- Classification: wrong specified behaviour (frontmatter).

### N2. Floating-art owner item omits the Cannon's sockets and gives a contradicted reason

- Where: line 157.
- Defect: `CAT-016-cannon.md:55` records both sockets hanging 3 cm above the foot and 12.8 cm off the jacket; line 157 lists only the track and the wheel. Coordinator decision 33 treats floating art with declared ports as a visible-solid repair (the Counter, part D).
- Fix: "For the owner, not blocking (recorded in deferred work; recommended default: add to repair part D, as coordinator decision 33 did for the Counter): the Cannon's track stands 4.5 cm above its foot and its two sockets 3 cm above the foot and 12.8 cm off the jacket; the Windmill's wheel 0.105 m in front of its nacelle; their ports are declared there."
- Classification: wrong specified behaviour (Code Map decision text).

Also update line 58 to cite this audit.

## Serialisation

Last of A, B, B2, C; the Baseball repair before or after; one publisher at a time on `VisibleSolidColliderTests.cs`. S6 moves the cited `WorkshopDeclaredPresentation.cs` lines; whichever of S6 and this part publishes second re-reads them (line 239). Shares `TODO.md`, `deferred-work.md`, `delivery-batches.md` with S3, S5, S5b, S6–S6f.

## Decisions for the owner

- Floating art (shared with part B's report): recommended default, add the Cannon's sockets and track and the Windmill's wheel to repair part D.

## Evidence limits

Nothing was built or run. `ImportedStep` (5e-5 m) assumes Godot stores imported positions within two 16-bit steps of the `.obj` values. The frames between construction or Reset and the first declared sample were not measured; C8 records the 0.0625 projection and the browser case is queued.
