---
title: 'Story 9R.6: Typed Mouth Snapping for Tubes (Reopened Stories 6.9 and 6.10; Story 6.11 Repair)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r6-typed-mouth-snapping-for-tubes-reopened-stories-69-and-610-story-611-repair'
  - '{project-root}/docs/planning/elements/CAT-048-pipe.md'
  - '{project-root}/docs/planning/elements/CAT-049-pipe_bend_45.md'
  - '{project-root}/docs/planning/elements/CAT-030-funnel.md'
  - '{project-root}/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Report #3. No snapping implementation exists.
- In `engine` and `ui`, the only hits are grid `Snapped(.1f)` (`ui/Workshop.cs:517`, `ui/WorkshopGuidance.cs:156,193`), the rotation gizmo snaps and status strings.
- Drawer placement commits a 0.1-grid pose with no mouth check (`ui/Workshop.cs:512-536`). `cat-030.test.ts:218` hand-stacks a funnel over a pipe with tilts and lifts.
- The scenes already carry `Loc_Port_mouth_start` and `Loc_Port_mouth_end` locators, but these are inconsistent:
  - **Axis.** The `mouth_start` locators of pipe, both bends and the powered gate are identity, so local +X points inward. Funnel `mouth_start` is rotated 180°, and every `mouth_end` faces outward. Evidence: `parts/scenes/pipe.tscn:176-180`, `pipe_bend_45.tscn:270-274`, `pipe_bend_90.tscn:446-450`, `powered_gate.tscn:217-221`, `funnel.tscn:438-442`.
  - **Position.** Pipe mouths sit at ±1.8, the collar centres. Every other part puts its mouth at the collar face (+0.09): funnel ±0.99, gate ±0.49, bend collars plus 0.09. CAT-048 fact 21 expects pipes 3.78 m apart, so the mouth is at ±1.89.
  - **Bore.** No locator carries a bore.
  - The extracted locators (`engine/gpu/GltfPhysicsBodyExtractor.cs:40-50`) are used only by `MachinePart.TryGetPortTransform` (`engine/MachinePart.cs:560-575`).

**Approach:**
- Mouths are declared on the element's own scene: a `Loc_Port_mouth_*` locator whose local +X is the outward axis, and `metadata/bore_radius` (f32 m).
- 6.5c extraction carries the bore. A missing bore on a mouth locator is an explicit error.
- A pure main-thread query, `TubeMouthSnap.Find`, proposes a snapped pose. The preview shows it without any command, and placement commits it, bypassing the 0.1 grid.

## Boundaries & Constraints

**Always:**
- Candidate mouths are within 0.45 m and 20° (DESIGN, CAT-048 facts 9–11), have equal bore (|Δ| ≤ 1e−6 m), opposing axes, and are free.
- The applied pose makes the mouths coincide within 1e−4 m, with dot(outward₁, outward₂) < −0.9999.
- Alignment is the shortest-arc rotation, which keeps roll.
- Snapping creates no network edge.
- It is idle-construction only: gated by `CanEdit` (`ui/Workshop.cs:56`).

**Never:**
- A per-element snap table or `PartId` switch (audit A3).
- Mutating any body in preview.
- Moving a locked part.
- Snapping while Running or Paused.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Snap | New pipe dropped 0.1 m off and 0.08 rad from a free pipe mouth | Preview snaps; on release, mouths within 1e−4 and dot < −0.9999 |
| Far | Mouth 0.72 m away | No snap; grid placement |
| Angled | 40° off | No snap |
| Occupied | Target mouth already joined | Not a candidate |
| Locked mover | Dragging a locked fixture | Not moved, no snap (`ui/WorkshopGpu.cs:75`) |
| Locked target | New pipe near a locked fixture's free mouth | Snaps (CAT-048 fact 21; joined_pipe, Story 9R.7) |
| Wrong bore | Pipe near the funnel's wide inlet (bore 1.3) | Not a candidate |
| Run/Pause | Placement attempt while Running or Paused | Unavailable; returns after Reset |
| Undo | One snapped placement, then ↺ Undo | Only the new part is removed |

### Decisions (coordinator delegation)

- **D1. Wrong-bore control.** The Funnel's wide inlet (epics, CAT-048 open question). Rationale: a real, visible part.
- **D2. Mouth geometry is scene data.**
  - Position and outward axis come from the locator. The axis rule is local +X.
  - Bore is the locator's `metadata/bore_radius`: 0.65 standard, 1.3 for the funnel inlet.
  - Scene fixes:
    - rotate the identity `mouth_start` locators of pipe, `pipe_bend_45`, `pipe_bend_90` and `powered_gate` by 180° about Y;
    - move pipe mouths to ±1.89.
  - Rationale: DESIGN.md:214 forbids hard-coded socket coordinate tables, and the `.tscn` is the authored asset (owner, 18 Oct 2026). Story 9A.17 keeps the locator names.
- **D3. Locked parts.** "A locked part" is refused as the mover. A locked part's free mouth remains a target. Rationale: CAT-048 fact 21 and Story 9R.7's joined_pipe require snapping onto fixed fixtures.
- **D4. Snapping also applies on releasing a moved, unlocked tube** (`ui/WorkshopGuidance.cs:449-453` → `CommitSelected`), not only on drawer placement. Rationale: CAT-048 §5 "separation and rejoin"; one gesture is one Undo.
- **D5. Tie-break.** The nearest distance wins, then the smaller angle, then the lower part id and mouth (start before end). Rationale: deterministic, and never guesses silently between equals (DESIGN.md:315).

</frozen-after-approval>

## Code Map

Line numbers are from the working tree on 18 Oct 2026, before Story 9.4b's commit. The symbol is authoritative.

- `ui/Workshop.cs`:
  - `CanEdit` `:56`;
  - `Click` placement `:512-536` (`PushUndo` `:516`, grid `:517`, `CaptureInstance` `:521`);
  - drag move `:450-465,540-546`;
  - `PushUndo` and `Undo` `:604-614` (whole-construction snapshot).
- `ui/WorkshopGuidance.cs`:
  - `CreatePreview` `:237`;
  - `_Process` preview follow `:266-294` (hidden in run `:287`);
  - release → `CommitSelected` `:449-453`.
- `ui/WorkshopGpu.cs:70-75` (`CommitSelected`, which refuses a locked part).
- `engine/gpu/GltfPhysicsBodyExtractor.cs:8-11,40-50`: `ExtractedPortLocator(PortId, Position, Rotation)`.
- `engine/PartDefinition.cs:54-97`: `PortLocators`. `EnsureExtracted` silently returns empty on a missing scene (X4, Story 9A.16). Snapping must not inherit that silence for tube parts.
- `engine/MachinePart.cs:560-575`: the string alias chain (report #12). It is not used for mouths.
- Scenes: `parts/scenes/{pipe,pipe_bend_45,pipe_bend_90,funnel,powered_gate}.tscn`, at the locator lines above.
- Save pose: f32 `Local` and `Rotation` (`engine/gpu/WorkshopWireFlatBuffers.cs:668-670,845-852`); `CanonicalRotation(float…)` (`engine/gpu/WorkshopConstruction.cs:16`).
- Suites:
  - `tools/e2e/cat-030.test.ts:218` (hand-stacked);
  - `cat-049.test.ts` and `cat-050.test.ts` (authored levels, numeric tilt via `tiltSelectedByFineRotate`).

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/TubeMouthSnapTests.cs` -- red first, with these native cases:
  - every tube part has two mouth locators, each with a bore, outward along local +X;
  - pipe mouths at ±1.89;
  - each matrix row through `TubeMouthSnap.Find`;
  - applied coincidence ≤ 1e−4 and dot < −0.9999 after the f32 cell/local/rotation capture round trip;
  - preview is non-mutating (construction is reference-equal and the revision is unchanged);
  - a mouth locator with no bore throws.
- [ ] `engine/gpu/GltfPhysicsBodyExtractor.cs` -- read `bore_radius` metadata into `ExtractedPortLocator` (a new `float? BoreRadius`). It is required, finite and > 0 for every `mouth_*` locator.
- [ ] Scenes -- the D2 locator fixes and metadata.
- [ ] `engine/TubeMouthSnap.cs` -- new. It is main-thread only, so it stays out of the workers (audit A1).
  - `Find(placed, mover, proposedPose) → SnapResult?` (target part and mouth, snapped f32 pose).
  - A mouth is free when no other placed mouth coincides with it within 1e−4 and opposed.
- [ ] `ui/WorkshopGuidance.cs` -- the preview uses the snapped pose when one exists, shows a snap highlight using the palette gold, and issues no command.
- [ ] `ui/Workshop.cs` -- placement: one `PushUndo`, the snapped pose replaces `at.Snapped(.1)` when present, the existing submit, and undo-pop on reject.
- [ ] `ui/WorkshopGpu.cs` -- D4 snap on release.
- [ ] `tools/e2e/tube-snapping.test.ts` -- new suite, through actual UI drag and drop. Cover:
  - each matrix row;
  - ball transit through straight→45°, 45°→45°, straight→90° and funnel outlet→pipe joints: the ball exits the far mouth, with no tick where its speed drops below 0.05 m/s inside the route, and its axial progress is monotonic;
  - Reset (9R.1 snapshot);
  - Save/Load (decoded mouth world positions still coincide).
- [ ] `tools/e2e/cat-030.test.ts`, `cat-049.test.ts`, `cat-050.test.ts` -- replace the hand-stacked joins with snapping placements, and add snapshot Reset.

**Acceptance Criteria:**
- Given declared mouth sockets (position, outward axis, bore radius) on the Pipe, both bends, the Funnel outlet and the Powered Gate, when the player places a tube part with a mouth within 0.45 m and 20° of a free compatible mouth (equal bore, opposing axes), then:
  - the preview shows the snap without mutating any body;
  - on release the mouths coincide within 1e−4 m and face opposite (dot < −0.9999);
  - one Undo removes only the new part.
- Given each negative control, then snapping is refused, and each refusal is proven:
  - a mouth 0.72 m away or 40° off;
  - an occupied mouth;
  - a locked part (per D3);
  - the Funnel's wide inlet (wrong bore).
- Given Running or Paused, then snapping is unavailable, and it returns after Reset.
- Given a straight→45°, a 45°→45°, a straight→90° and a Funnel outlet→pipe joint, then a ball crosses each continuously with no snag (positive, per CAT-048 fact 9).
- Given a joined placement, then it survives Reset (snapshot) and Save/Load.
- Given Chrome, when `tools/e2e/tube-snapping.test.ts`, `cat-049`, `cat-050` and `cat-030` run serially, then all pass 100%.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings;
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

Amelia-12, 20 Oct 2026, on base `007a65ed` (Story 9R.5b). Gaps decided under the owner's pace rules:

- **I1. Six tube scenes, not five.** `ball_detector.tscn` also carries `Loc_Port_mouth_*` locators (CAT-002 "Sockets and ports": Start faces −X, End +X, common bore). Its `mouth_start` is flipped like the others and both mouths declare bore 0.65, so it snaps by data like every tube. Otherwise the required-bore rule would break registry discovery.
- **I2. Mouth ends are typed.** The extractor parses `mouth_start` / `mouth_end` into `TubeMouthEnd` at the scene boundary and rejects any other `mouth_*` name. `bore_radius` must be a finite, positive float; a missing, non-numeric or invalid value is an `ArgumentException`. `ExtractedPortLocator` gains `Mouth` and `BoreRadius`.
- **I3. The 19 Oct mouth-facing finding is fixed here.** Every start mouth now faces outward. The native test checks this from the geometry (`outward_start · (start − end) > 0`), not from the matrix.
- **I4. One placement pose.** Both the preview and the click call `PlacementPose`: the 0.1 grid point (`at.Snapped(.1)`) and the chosen preview orientation, replaced by the snap when one exists. The preview used to show `ClampPlacement`'s unsnapped plane height while the click committed the snapped one; both now match. Rotating the preview starts from the chosen orientation, never from a snapped pose.
- **I5. D4 is applied only on the drag and lift release** in `_Input`. Rotate, depth, gizmo and Height steps never snap. While a tube is dragged, the gold ring shows its release joint, and the body is unchanged until release.
- **I6. Preview Height steps.** The ↑/↓ Height row stepped the preview from its stale node height (y 2 until the pointer first leaves the menu). It now steps the placement plane. This is a player-visible defect on the placement path, found by the suite.
- **I7. Locked parts in Chrome.** No current level has a locked tube; that arrives with `joined_pipe` in Story 9R.7. The locked-target snap (CAT-048 fact 21) and the locked-mover refusal are proven natively through `TubeMouthSnap.Find`. Chrome proves the UI gate: dragging the clear_pipe level's locked receiver issues no command and moves nothing.
- **I8. cat-049 and cat-050 have no hand-stacked joins.** Each level stocks one bend, so there is no join to convert. They stay as regression suites and already carry the 9R.1 snapshot Reset. cat-030 test 5, the real hand-stacked join, now snaps a pipe to the Funnel outlet and asserts the join from the saved construction.
- **I9. Continuity.** Natively, the production Simulation is stepped every tick: speed ≥ 0.05 m/s and chord progress never falls back by more than 1 mm, from the entry mouth until the ball is 0.4 m past the far mouth. Chrome checks the same on every observed pose-ring publication; it cannot see every tick.
- **I10. Driver.** New helpers: `expectNoCommand`, `hoverCanvas`, `adjustPreview` (UI-only; the menu closes through its own button, because Escape cancels the tool), `liftSelectedByHeight`, `dragPart`, `dragPartRefused` and `undo`. Every acknowledged command now clears `paletteScrolled`, because the palette rebuild can move its scroll; a post-Reset `selectTool` once chose the wrong row.

## Spec Change Log

## Review Triage Log

## Design Notes

**Legacy deleted:**
- the click-only tube placement with no mouth check;
- the hand-stacked join in `cat-030.test.ts:218` and its "no snap" assumption;
- the inconsistent identity `mouth_start` locators;
- the pipe's collar-centre mouth positions.

**Test-first plan.**
1. The native `TubeMouthSnapTests` fail first.
2. Chrome proof: `tube-snapping`.
3. Negative controls: far, angled, occupied, locked mover, wrong bore, and Run/Pause.
4. Boundaries:
   - 0.44 m snaps and 0.46 m does not;
   - 19° snaps and 21° does not;
   - the bore tolerance.
5. Run/Reset uses the snapshot.
6. Save/Load covers the joined pose.

**Mutation plan:**
1. Threshold 0.72 m, so the far control snaps and fails.
2. Drop the bore check, so the funnel inlet snaps.
3. Skip the occupied check, so the occupied control fails.
4. Leave one `mouth_start` locator unflipped, so the axis test and opposed-dot assertion fail.
5. Apply the grid snap after the mouth snap, so the coincidence assertion fails.
6. Preview submits a Construct, so the non-mutating assertion fails (a CCGPU acknowledgement during hover).

**Impacted suites:**
- Native: `StraightPipeTests`, `PipeBend45Tests`, `PipeBend90Tests`, `FunnelTests`, `PoweredGateTests`, `BallDetectorTests` (shared bore), `PartRegistryTests` (locators).
- Chrome: tube-snapping, cat-048a, cat-048b, cat-049, cat-050, cat-030, cat-051, cat-002, then the full serial run.

**Risks.**
- **Moving the pipe mouths** changes nothing physical, since colliders are separate. It does change any test that reads `GetPortTransform("mouth_*")`.
- **Ball snag at seams.** Collar contact at seams may snag. A snag is a physics finding to report, not a reason to loosen the threshold.

**Refresh after Story 9.4b lands.** 9.4b does not touch the placement UI. The Chrome gate inherits the cat-023b clock fault status.
