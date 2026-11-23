# EL-019 · Water check valve — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-019 delivery slice](../invest/named-elements.md#element-019). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-019 · Water check valve · Water |
| Anchors | [element-019](../requirements.md#element-019), co-anchored `sequence-task-572`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-019](../invest/named-elements.md#element-019), owner S440 |
| Disposition | To deliver as a single element |
| Planned package | id `check_valve`, directory `elements/CheckValve/`, title "Check valve" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | Pipe kit [EL-008](EL-008-straight-water-pipe.md) to [EL-012](EL-012-water-pipe-cap.md), [EL-002](EL-002-header-tank.md), [EL-022](EL-022-water-pump.md), EL-024 |

## Declaration

Closest delivered shape: the Clear pipe (a tube with two mouths).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body.** One static body, mass 0. Axis local X; origin at the centre.
- **Colliders (two).** Tube: bore 0.10, outer radius 0.14, length 0.40. Bulge: solid Cylinder, radius 0.15, length 0.16, axis X, at the centre (the kit rule: nothing reaches more than 0.15 m below the axis).
- **Liquid node.** One fitting with one **one-way seat** (L14): full bore area A₀, direction `Start` → `End`, cracking head h_c = 0.02 m. Q = K₀·√(Δh − 0.02) while the head at `Start` exceeds the head at `End` by more than 0.02 m; otherwise 0 in both directions. There is no stored open or closed state: the flow falls smoothly to zero at the cracking head, so nothing can chatter.
- **Mouths (pipe bore).** `Start` at (−0.20, 0, 0), normal −X, Intake; `End` at (+0.20, 0, 0), normal +X, Discharge. They are not interchangeable.
- **Parameters.** None. The cracking head is fixed: small enough that any real head opens the seat, large enough to be a visible final difference (0.02 m is two level tolerances).
- **Presentation.** `Visual/Flap` `LocalRotationZ` from the committed seat flow: seated at 0, open 60° at 0.02 m³/s and above, clamped (W12, missing).
- **Palette.** `ElementCategory.Control`; palette order 412 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | Tube 0.40 long, cream `#fff8e9`, navy flush end bands | solid, hollow; the Tube collider | body pose (static) |
| `Visual/Bulge` | Cylinder radius 0.15, length 0.16, cream | solid; the Cylinder collider | body pose |
| `Visual/Arrow` | Gold `#f7cb52` arrow along the bulge, pointing `Start` → `End`, flush | decorative: printed flat | nothing |
| `Visual/Flap` | Gold flap glyph in a printed window on the bulge, flush | decorative: printed flat, moved by a binding | presentation binding |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.10 | none | nothing |

Icon: a pipe with a round bulge and an arrow. It differs from the Tap (square bonnet, lever) by the round bulge and arrow; direction reads from the arrow's shape.

### Behaviour and numbers

- **Forward.** Two Water tanks on the bench, A full (1.0 m³) and B empty, joined pipe–valve–pipe with the arrow from A to B. Three apertures in series (A's mouth join, the seat, B's mouth join): K = K₀/√3 = 0.04820. Liquid flows until the levels differ by the cracking head: A at 0.51 m, B at 0.49 m. With u = Δ − 0.02, du/dt = −2·K·√u / A, so it takes √0.98 / 0.04820 = **20.5 s**.
- **Reverse.** With the valve turned round, or with B full and A empty, the head is against the seat: nothing flows, at any head. "Reverse head cannot flow through a closed seat."
- **Equal levels.** Nothing flows; a difference under 0.02 m in the forward direction also passes nothing.
- **With a pump.** Downstream of a pump ([EL-022](EL-022-water-pump.md)) it passes the pumped flow. A stopped pump is an open edge (L15), so without this valve the lifted liquid runs back down; with it, the liquid stays. This is the "prevent backflow" decision of sequence-task-389, and EL-022 row M7 proves it with a control that fails without the valve.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube; W21 Cylinder collider | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting and mouth roles |
| W3 tube mouth and snap | narrower | join not compiled |
| W7 fitting and flow | missing | `ENGINE-LIQUID` |
| W14 one-way seat with a cracking head | missing | `ENGINE-LIQUID` (gap in its text) |
| W12 driver for a committed seat flow | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 3 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two colliders, one fitting with a one-way seat, two mouths, one binding. The earlier `CheckValveState { Seated, Open }` is gone: there is no state to hold.
2. **Generic laws.** The one-way seat is a declared property of an aperture (L14), available to any part. "Pressure difference opens the permitted direction" is the head difference in L7.
3. **Three layers.** *Colliders:* Tube and Cylinder. *Visuals:* the table above; arrow and flap are flush. *Animation:* the flap reads the committed flow.
4. **One-way flow and typing.** Mouth roles are enums; the direction is which mouth is `Start`.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Tube, 1 Cylinder); joints 0; drives 0; sensors 0; liquid nodes 1 fitting; seats 1; mouths 2; bindings 1; parameters 0.
6. **Performance fitness.** Two colliders, fewer than the Clear pipe's three; the bulge is a visible solid. One aperture row.
7. **Tests are scenarios.** Rows CV1 to CV6.

## Sources and legacy

- Requirement: "Pressure difference opens the permitted direction and closes reverse flow"; outcome "Reverse head cannot flow through a closed seat." No variants.
- Refinement [S707](../invest/refinements.md#s707) check-valve; [sequence-task-389](../requirements.md#sequence-task-389) "prevent backflow"; family boundary [fluids](../invest/profiles.md#fluids).
- **Legacy search.** No liquid check valve. Gas analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The gas nozzle supports forward flow only; equal pressures give no flow; a reverse pressure throws. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L57` | Carry forward "equal head gives no flow". Not the exception: reverse head gives zero flow. |

**Files harvested:** none of its own.

## Acceptance outline

Authority: [element-019](../requirements.md#element-019). Tolerances L17. Planned class `CheckValveTests`. Rig: two Water tanks joined pipe–valve–pipe.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| CV1 | Given A full, B empty and the arrow from A to B, when Run advances until flow stops (stall guard 62 s), then A holds 0.51 m³ and B 0.49 m³ within 0.01 m³ and the total is unchanged (the level declares a Run length of 65 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17) | `CheckValveTests.ForwardHeadFlowsUntilTheCrackingHeadRemains` | CV2 | flow has stopped by 20.5 s within 10% |
| CV2 | Given the valve turned round in the same rig, when Run advances 20 s, then A keeps 1.0 m³ and B 0 | `CheckValveTests.ReverseHeadDoesNotFlow` | CV1 | — |
| CV3 | Given A empty and B full with the arrow from A to B, then nothing flows | `CheckValveTests.NothingFlowsBackThroughTheSeat` | CV1 | — |
| CV4 | Given A at 0.515 m³ and B at 0.485 m³ (difference 0.03 m), then liquid moves; given 0.505 and 0.495 (0.01 m), nothing moves | `CheckValveTests.TheSeatOpensOnlyAboveItsCrackingHead` | — | equal volumes: nothing moves |
| CV5 | Given `End` unjoined and A full, then the valve's `End` discharges as a free end; given `Start` unjoined and liquid offered at `End`, nothing passes | `CheckValveTests.MouthRolesFollowTheArrow` | — | — |
| CV6 | Given a run, then Reset restores both tanks; save and load keep the valve's orientation and both joins; a ball dropped on the bulge rests on it | `CheckValveTests.ResetAndSaveLoadKeepTheOrientation` | — | — |

Generic cover: W22. Later-partner row: holding pumped liquid when the pump stops is [EL-022](EL-022-water-pump.md) row M7. Chrome recipe (pending): two tanks joined by pipes with a Check valve snapped between, arrow from the full tank to the empty one.

## Open questions

All resolved; none needs the owner.

1. Cracking head fixed or authorable: fixed at 0.02 m (D13, D19).
2. Reverse leakage: zero; the requirement says reverse head cannot flow.

## Integration

- **Works with.** Planned: EL-001, EL-002, EL-008, EL-022, EL-024. None delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-338`, shared by seven identities; `sequence-task-572` not co-anchored with a todo; no `s440` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID` including a one-way seat; EL-001; EL-008; S7.
- **Files.** Adds `elements/CheckValve/CheckValve.csproj`, `CheckValveElementDeclaration.cs`, `check_valve.tscn`, `icon.svg`, `CuriousContraptions.tests/CheckValveTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A one-way seat implemented as a stored open or closed state; mouth roles not enforced; a seat that leaks in reverse.
