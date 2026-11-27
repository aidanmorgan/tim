# Planning audit 5: ELEMENTS-VISIBLE-SOLID-REMAINDER part C (Cannon, Windmill, Balloon)

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md` (5,744 words, within 6,000). Checkout `.claude/worktrees/slice-visible-solid-c`, HEAD and baseline `d37c0b98` (`origin/main`, part B2 published); product and test paths at the baseline, planning records uncommitted. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning, dispatch re-audit (coordinator decision 38); read-only. Nothing was built or run.

## Verdict: Scoped Pass with non-blocking corrections

No finding is inside `<intent-contract>`. The capability probe is complete: every *present* item has a cited symbol and an exercising test; the *present, untested* first-sample path is owned here (C8); the two *narrower* matcher items are owned here (G1, G2); the projection gap is owned by `ENGINE-ONE-WAY-INSTALL` under owner decision 5's conditional; no required-now owner decision is open. Tier: **engine** (shared harness code in `VisibleSolidColliderTests.cs`; narrower and present-untested capabilities). Apply N1–N8 before the implementer launches; none needs a re-audit.

## Findings

### N1. Stale `BalloonTests.cs:152` (test)
Where: Code Map "Not consumers" (line 73) and the C8 row (line 213). `:152` is now the `WorkshopPhysicsCompiler.Compile` line; the 0.1 mm preparation bound is `:158–159`. E2 recorded the move; E3 did not apply it. Fix: cite `BalloonTests.cs:158–159` in both places.

### N2. C8 must name the schedule entry point (test)
Where: task 1 and the C8 row. The probe's build-mode claim rests on `WorkshopDeclaredPresentation.Advance(WorkshopSchedule, PhysicalTime)` (`:637`), which holds the anchor when not running. The single-argument `Advance(PhysicalTime)` (`:651`), used through `AnimationTestTime` by `BalloonArtTests.cs:29`, bypasses the schedule, so a test written that way would not prove the claim. Fix: state that C8 uses `harness.Schedule` (`WorkshopIntegrationHarness.cs:89`; it throws "No schedule installed" if none, which is outcome (a)) with `Presentation.Advance(harness.Schedule, master)`. The remaining entry points exist: `PartRegistry` and `EnsureConstructed` (`BalloonArtTests.cs:12–17`); `WorkshopIntegrationHarness.Building` (`:178`; `Presentation` is built on the Construct response, `:303–307`); `BalloonTests.Balloon(…, radius:)` in `TennisBallTests.Scene`; `ApplyDeclaredProperty` filtered by owner (`BalloonArtTests.cs:30`, as `MachineWorld.Gpu.cs:251–252`); `Run`; `Reset` with `ResetPhysicalVisuals` (`CannonArtTests.cs:47`, as `MachineWorld.Gpu.cs:317`); the expected radius from `harness.Simulation.Scene.Physics.Colliders` (as `CannonIntegrationTests.cs:444`) `.Radius.Value` (as `BalloonTests.cs:157–159`).

### N3. G2 replaces the authored scale (wrong specified behaviour, clarification)
`BodyPose` composes `local.Basis * Basis.FromScale(scale)` (`:412`), and the Envelope's own transform already carries scale 0.36 (`balloon.tscn:39`). Reusing that composition draws 0.36 · r. Fix: G2 "sets the node's scale to the value, replacing the authored scale (rotation kept)". The default-parameter check would fail loudly otherwise, so not blocking.

### N4. AC2 leaves the rest of the two changed entries unchecked (test)
AC2 is expressible on the schema: base colliders are `Colliders`, the rotor's are `Mechanical.MovingBodyColliders[0]`, occluders are `Mechanical.RotaryAirflowCaptures[0].InternalOccluders`. Fix: add "and every other field of both entries is equal, including the Windmill's `MovingBodyColliders[0]` (five)".

### N5. Theory text and tuples the drops change (test)
The low-restitution theory's summary (`VisibleSolidColliderTests.cs:737–743`) names only the Conveyor and Linear pusher and says each feature's control stops "at least 0.169 m lower"; C1's control is 0.14 m. The `Feature` summary (`:582–585`) lists the feature kinds. The C1 and C5 tuples omit `Drop`'s sixth field. Fix: task 5 updates both summaries ("at least 0.14 m") and gives Restitution 0 for both (`CannonElementDeclaration.cs:25`, `WindmillElementDeclaration.cs:25`).

### N6. Two more stale comments; summaries (test)
`:134` (domino: "The pips: engine-multi-collider-dynamic", but the pips are in the row's Solid list) and `:142` (weight: base Solid, bands and eye Hollow) are the same stale kind as XB-04. Under the 4 November repair policy, add them to task 5 and record them in task 7. Task 2 lists the class, `ArtSpec` and `Art` summaries but not `Solid` (`:67–74`), which the owned scope includes and G1 changes (`Inner`, `Allowance`). The message at `:259` ("not an open cylinder or a torus") should also name an imported ring.

### N7. C5's drop point lies under a decorative spoke (test)
`Spoke0` (0.4 × 0.03 × 0.02 at Output z 0.065, `windmill.tscn:206–208`) spans local x 0.05–0.45 at y −0.1. Its top is at z 0.515, 15 mm proud of the 0.5 face. C5's contact point, local (0.35, −0.1, 0.5), is under it. The test still measures the collider within the 17 mm envelope, but the resting ball is drawn 15 mm into the spoke. Fix: move the drop off both spokes. A 0.34 m ball clears a 15 mm spoke only about 0.1 m from the contact point. For example, X 0.37 and Z 0.22: the contact is 0.17 m from the wheel's centre at 45°, 0.105 m from each spoke edge, and 0.458 m from the guard. The implementer recomputes these clearances. Alternatively, record the overlap in the C5 row as decorative under part A's D5.

### N8. Element documents (wrong specified behaviour, records)
E2's old→new map was re-read line by line and is correct. Stale citations it missed:
- CAT-003:100 `FanGuidanceTests…(:45)` → `:53`.
- CAT-003:72 and EL-208:70 `FanGuidanceTests.cs:16` → `:18` (`:16` is an `InlineData` row).
- EL-208:70 `(:124)` → `:132`.
- EL-208:50 `spec-engine-art-solidity.md:92` → `:93` (`:92` is the tennis row).
- EL-208:149 `GasCouplingValues.cs:10` → `:11`.

One content claim is wrong now: EL-208:48 calls the string "the one non-solid strand", but owner decision 1 keeps thin wires non-solid too (CAT-003:57 says two).

Lines delivery makes false, which task 7 rewrites, not only re-cites:
- CAT-016:27 (colliders row; `cannon.tscn:132–:155` must extend to the new proxies), :49, :52 and :53 (defect rows), :55 and :212 ("blocked on parts B and B2"), :220, :224 and :225 (five, "9 after part C").
- CAT-070:29 (support colliders), :33 ("nine colliders" exempt), :52 and :55, §8, the conformance verdict and points 3, 5 and 6 (:123–124).
- Every CAT-070 `Scene:` citation (`:76–:84`, `:88`, `:121–:184`): these shift when `Shape_Output` and `col_output` are inserted.
- CAT-003:57 and :59 (record the O1 outcome) and EL-208:52.

The carry-forward `deferred-work.md:380` describes only part B2's shift. Part C moves `VisibleSolidColliderTests.cs` citations again, so task 7 amends that entry (same owner rule).

Minor: run log E2's `RotaryAirflowValues.cs:14–18` is `:14–17` (line 18 is blank).

## What was re-verified at `d37c0b98`

- Every contract `path:line` in the Code Map, probe, Repairs, Matcher changes, Footprint, Tasks and Verification, and every E2 citation, is correct except N1 and the nit above.
- (a) Proxies bake with no refusal. `BakeProxy` (`SceneGeometryExtractor.cs:99`) admits `MeshInstance3D` box and sphere meshes and a `CylinderShape3D` with explicit `radius` and `height`. The only name refusals are `col_capsule`/`col_hull` (`:104–106`) and the `-col_` infix (`:171–177`). `Hollow` (`:197–238`) returns null for these shapes. `BakeBodies` (`:56–67`) appends per owner in file order, so these become Cannon base colliders 5–8 and Windmill base collider 4. `load_steps` 20 stays in `cannon.tscn` and becomes 21 in `windmill.tscn` (19 resources today).
- (b) `new(new(0), new(4))` is resolved by `AirflowDomain.Collider` (`BundleMaterial.Shapes`, body 0 = `Colliders`) and `CompiledIdentityMap.Collider` (`:62`). The rotor is body 1 (`MovingBodyColliders[0]`). Admission has no count cap. The wheel blocks the rear Fan's ray to the (−0.12, 0, 0.42) sample at local x 0.03–0.18, so `Exposure.Rear` (−6 rad/s) and `EqualOpposition` discriminate.
- (c) See N4. (d) `ParameterSets` (`:229–235`) sweeps `radius` to 0.18 and 0.5 (collider bindings `BalloonElementDeclaration.cs:45–48`).
- (e) Restitution is the first `MaterialProperties` field (`ColliderDescriptor.cs:21–25`) and is 0 for both parts. C1: apex 0.72, breech 0.14 m lower, rear collar 0.495 m and jacket 0.564 m from the resting centre (radius 0.34). C5 (`Back`): face 0.5, nacelle 0.225 m lower, guard 0.436 m, rotor blades 0.385 m.
- (f) Feasible natively; see N2.
- (g) G1 uses Godot `ArrayMesh` arrays in the test. G2 uses public `IsobaricGas.OuterRadius`, `MassScalar.Evaluate(IReadOnlyDictionary<ParameterId, float>)` (`MassFrameValues.cs:12`) and the chain `LocalColliderReference` (`ElementPresentation.cs:110`) → `GasShapeDescriptor.Store` (`GasCouplingValues.cs:11`) → `GasStores` (`BundleSchema.cs:46–47`). Neither touches an AC5 path.
- (h) `.godot/imported` is absent (only `.godot/mono`). `CuriousContraptions.tests.csproj:79` and `CuriousContraptions.2dog.csproj:31` set `GodotProjectDir`, and 2dog.engine's `TwoDogImportGodotProject` runs after Build with `TwoDogAutoImport` defaulting to true. So the capture's solution builds import the assets (the web build's `TwoDogAutoImport=false` does not matter). With no import capability the target only warns, which the capture's zero-warning expectation catches. The native fixture links `.godot` into its project.
- (i) Part B2 measured that one `Specs` row adds one counted case (7415 → 7417, row plus fact). `Feature` and float `InlineData` serialize. So the full round is 7431 + 10 = 7441. The registration filter (`delivery-workflow.md:69`) selects none of the three changed classes: 829 + 0.
- XB-04 (`completion-ledger.md:318`): `:87` is stale (the marks are 0.02 m boxes, `battery.tscn:30–32`; the redraw is `batch-b.md:77`), and so are `:126`, `:147` and `:179`. Tasks 5 and 7 carry them.
- Rules. E1, E4, E6, E7, E8 and E9 apply; E2, E3 and E5 apply only as stated (no runtime numeric code; nothing superseded). D1, D3, D4, D5 and D7 hold; D2 and D6 do not apply. Forward-only: G1 is one reader for a new input kind, and G2 is one rule for one driver; neither is a fallback or a second path. Element-free: no harness rule names a package.
- Tolerances. `Tol` is unchanged. `ImportedStep` (5e-5 m) is derived from 16-bit position storage (guard 1.63 m → 2.5e-5 m a step) and is not fitted. 0.1 mm reuses the preparation bound. The drop window reuses the theory's envelope. No unjustified precision.
- Contract budget: 5,744 words.

## Evidence limits

Nothing was built or run. Whether Godot compresses the imported positions, and how C8 resolves (O1 outcome a or b), are known only after task 0 and task 1. Retained behaviour (C4, C12) is shown here by geometry and by reading, and is proven by the full round.

## Confirmation of corrections (26 November 2026)

**Final verdict: Scoped Pass.** The coordinator's amendments (run log E4) were re-read in the working-tree contract (5,871 words, within 6,000). The `<intent-contract>` block is byte-identical to HEAD.

- **N1.** The C8 row cites `BalloonTests.cs:158–159`. The Code Map's `:152` ("is untouched") is kept. It is the `Compile` call in the same test, `NondefaultLiftAndStrongDragChangeStandardMotion`, so the statement stays true. I accept this deviation from the fix above.
- **N2.** The C8 row uses `Presentation.Advance(harness.Schedule, …)` (`:637`). A build-mode harness without a schedule is outcome (a).
- **N3.** G2 replaces the authored scale and keeps the rotation.
- **N4.** AC2 requires every other field of both entries to be equal, including `Mechanical.MovingBodyColliders[0]`.
- **N5.** Task 5 updates the `Feature` and theory summaries (`:582–585`, `:737–743`). The C1 and C5 tuples carry restitution 0.
- **N6.** `:134` and `:142` are in the owned scope and task 5. Task 2 covers the `Solid` summary and `:259`.
- **N7.** The C5 drop moves off both spokes, for example X 0.37, Z 0.22, with a 5 mm floor and a return to planning. Rechecked: the nacelle control (0.225 m lower) still applies at local (0.37, −0.22). The guard is 0.458 m from the resting centre, and each spoke edge 0.105 m from the contact.
- **N8.** E4 records the missed citations and the lines delivery makes false. Task 7 cites E2 and E4 and amends `deferred-work.md:380`.

No new defect was introduced, and no finding remains.

## Addendum: amended G1 (26 November 2026)

**Verdict: Scoped Pass for the amended design answer.** Reviewed: contract G1 and Spec Change Log 2, run log E6 departure 1 and E7, and `VisibleSolidColliderTests.cs` (`ImportedStep`, `ImportedSpread`, `Annulus`, `Stored`, `RingError`, `Imported`, the C10 fact).

- **Justified independently, not fitted.** The premise is unchanged: each stored position lies within `ImportedStep` of its true value. A vertex and the measured extreme it is grouped with are therefore each within one step of the same true circle or end. By the triangle inequality they differ by at most 2 · `ImportedStep`. `ImportedSpread` follows from the premise, not from the observed 5.74e-5 m. The observation is consistent with the premise: each extreme lies within 2.7e-5 m of its literal, and the farthest vertex about 3e-5 m from the true circle, both inside one step. The spread is used only for grouping, where `Annulus` compares two stored values.
- **One step stays wherever a stored value meets an exact one.** It is kept for the literal checks (`ReadRing`), the row allowance `Tol + ImportedStep`, the wall floor, and the centroid floor. The centroid floor needs no spread, because `inner` is the measured minimum, so every vertex already lies at or beyond it.
- **Controls still refuse.**
  - The 0.6 × 0.2 × 0.6 box has every corner at one radius, so its wall is zero.
  - The capped cylinder has a centre vertex, so its inner radius is 0.
  - The half ring passes grouping but reads with a mesh error near its outer radius, far above 5 mm.
  - The built full ring reads as its literals, so the builder is not the cause of those refusals.
  - A plausible non-ring with a vertex anywhere across a real wall, such as a flange, step, chamfer or subdivided cap, is more than 1e-4 m from both bands and is refused.
- **Observation, no change required.** The inner and outer bands overlap only when the wall is under 2 · `ImportedSpread` (0.2 mm). A mesh passing there is still a drawn thin ring, and it matches only a tube of identical size. A flat zero-thickness washer reads with half-length 0 and fails its row loudly. Neither can admit a wrong collider.
- **Nothing else in the contract needs to change.** The C10 row and Spec Change Log 2 already state the amended rule. `ImportedStep`'s stated reason still holds. The first evidence limit above (how Godot stores imported positions) is now answered by run log E6.
