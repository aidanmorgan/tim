# EL-213 · Optical combiner — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the outlet direction.

EL-213 refines the catalogue combiner [CAT-006 beam_combiner](CAT-006-beam_combiner.md), whose spec holds the full legacy harvest. This file restates the declaration and the named identity's acceptance: compatible incident channels leave one explicit output aperture with conserved combined power and declared coupling loss; a missing channel is never synthesised, and crossing unrelated beams never combine by object identity.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-213 |
| Name | Optical combiner |
| Type | Optical |
| Anchor | [requirements.md#element-213](../requirements.md#element-213); named entry [named-elements.md#element-213](../invest/named-elements.md#element-213); source record [todo-299](../requirements.md#todo-299); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S509 |
| Refines CAT | [CAT-006 beam_combiner](CAT-006-beam_combiner.md) (`beam_combiner`, "Beam combiner"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-174 prism (inverse); EL-149 to EL-152 mixed receivers (consumers); EL-176 to EL-178 lasers and EL-143 to EL-145 filters (inputs); EL-212 mirror |
| Roadmap story | 13.5 Planar Reflection Mirror, Beam Splitter & Beam Combiner (CAT-041, CAT-008, CAT-006) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cyan cube 1.4 × 1.4 × 1.4 m at the origin (half-extents 0.7); opaque navy base 1.8 × 0.18 × 1.8 m at (0, −0.85, 0). Pick radius 1.3 m. Static `RigidBodyDeclaration`, two box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
- **Constraints and joints:** none.
- **Typed ports (optical, closed enum `OpticalPortId`):**
  - `First` at (−0.76, 0, 0), normal −X; `Second` at (0, 0, 0.76), normal +Z; `Third` at (0, 0.76, 0), normal +Y. Each a finite disc radius 0.43, interaction Route, transmission (0.9, 0.9, 0.9), front incidence only. The inputs identify ports, not required colours: all accept any RGB.
  - Outlet (0.76, 0, 0), direction +X.
  - No electrical or activation ports.
- **Sensors and activation:** none (input power and output power are observations, not controls).
- **Work and energy stores:** none. Each incoming ray is routed separately to the outlet with power × 0.9 and its own remaining range minus internal travel and its own interaction count; co-linear exits sum at downstream receivers. No stored or new light.
- **Parameters:** none. Coupling loss 0.1 per ray is a fixed declaration constant. Requirement mode: declared geometry only.
- **Cosmetic curves and UI bindings:** output lens eases toward the actual exiting light colour (beam-ink rule) or slate `#556573` when dark, at exponential rate 12 /s; light exhausted inside the body cannot light the outlet. Scalar observations: output R, G, B in game optical power. Selected-only preview with composition MergeCollinear: co-directed paths from the same origin sum over their actual overlap; crossing paths stay separate.
- **Art (DESIGN.md palette):** cyan cube `#66b8c9`, navy foot `#293954`, three cream-rimmed `#fff8e9` input discs (ring r 0.48, tube 0.055; slate lens r 0.43) carrying one, two and three cream bars; smaller ochre-rimmed `#e8b764` outlet (lens r 0.34, rim r 0.40); DESIGN.md "Combiner and mixed-channel receivers".
- **Catalogue and inventory entry:** id `beam_combiner`, title "Beam combiner", category Optics, colour (0.40, 0.72, 0.79); description "Aim beams into any of the three cream-rimmed inputs. Their existing colours share the gold-rimmed output, retaining 90% power. No electricity or new light is produced." Icon `ui/WorkshopIcons.cs@a6c914e:L80-L80` (current, kept).

### Variants

One. One, two and three active inputs are proof cases of this element, not variants.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction; source-specific composition: shared spectral allocation combines only admitted incident power and a real output aperture, never duplicate source energy).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).

**Missing**
- OpticalTransport Route interaction with directed outlet, internal travel, per-ray budgets and outlet accounting at the captured pose — Story 13.5. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation, S486 optical-commit, S488.
- Shared beam renderer merge rule — Story 13.5 (presentation).
- OpticalAbsorption/SensibleHeat for the 10% loss — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** lasers (CAT-036 or EL-176 to EL-178; Story 13.1) and filters (EL-143 to EL-145; Story 13.4) as inputs; mixed receivers (EL-149 to EL-152; Story 13.6) as consumers; CAT-005 battery; CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-213](../requirements.md#element-213): compatible incident channels leave an explicit output aperture with conserved combined power and declared coupling loss; a missing channel is not synthesised; crossing unrelated beams do not combine by object identity. Campaign: first use 51–60; practice 71–80; reuse 117 and 142 ([campaign-reservations](../invest/campaign-reservations.md)). [todo-299](../requirements.md#todo-299): three cream input rims, gold output rim, numbered marks; each ray keeps 90% power and its remaining range/interaction budget, counting internal travel. CAT row [current-cat-006](../requirements.md#current-cat-006).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); full harvest in [CAT-006-beam_combiner](CAT-006-beam_combiner.md#legacy-harvest); key facts):
1. Retention 0.9, outlet, three Route apertures — `parts/BeamCombinerPart.cs@a6c914e:L12-L20`. Carry forward.
2. Input power aggregate, output power from the outlet, Active only when output > 0; output lamp follow rate 12; preview MergeCollinear — `parts/BeamCombinerPart.cs@a6c914e:L21-L49`. Carry forward behaviour; not the per-part callbacks.
3. Art and port marks — `parts/BeamCombinerPart.cs@a6c914e:L50-L75`. Carry forward.
4. Route law and outlet accounting — `engine/OpticalNetwork.cs@a6c914e:L119-L125`, `engine/OpticalNetwork.cs@a6c914e:L171-L182`. Carry forward; not the CPU solve.
5. Catalogue entry — `parts/catalog/beam_combiner.tres@a6c914e:L6-L11`. Carry forward.
6. Acceptance: the outlet lamp shows only light that actually exits (range 2.1 short: input read, output 0) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L16-L33`; three filtered amber lasers give a white receiver laser power × 0.9 (±1e-5) with one outgoing segment, for rotated and reordered sets; Reset clears everything — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`; each input keeps its remaining range (total path 5 ± 0.001 including internal travel) and never creates channels — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward.
7. Acceptance: a three-mirror loop back into the combiner keeps one 16-interaction budget with strictly falling power — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L138-L160`; artwork sums shared paths only over their overlap and keeps crossing beams separate — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L161-L177`; back face and housing block, and a wall on the output still occludes — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L178-L199`. Carry forward.
8. Acceptance: output channels publish atomically across rollback, pause, Reset and Save/Load; output = 0.9 × input (±1e-5) — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`. Carry forward.

**Files harvested:** `parts/BeamCombinerPart.cs`, `parts/catalog/beam_combiner.tres`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a battery, three triggered lasers with red, green and blue filters (or EL-176 to EL-178) aimed at the three inputs, and a white receiver on the outlet; wire with the connection UI; aim with the gizmo. Run.
- **Positive:** the receiver reads each input channel × 0.9; the outlet lens shows the mixed colour.
- **Negative / controls:** (named) remove one input — its channel is absent at the outlet (not synthesised); two beams crossing near the combiner without entering an input stay separate; beam on the back or housing does not route; short range exhausted inside the body leaves the outlet dark.
- **Boundaries:** one, two and three inputs; per-ray range including internal travel; interaction budget in mirror loops.
- **Run/Reset:** Reset clears paths, input and output power and the lens. **Save/Load:** placement and orientation round-trip.
- **Integrations:** practice 71–80 and reuse 117, 142; mixed receivers; prism recombination. Story 13.5 suite `tools/e2e/cat-041-008-006.test.ts`. Binding criteria: [element-213](../requirements.md#element-213), [current-cat-006](../requirements.md#current-cat-006).

## 6. Open questions

1. Story 13.5 says combiners "merge orthogonal beams into one"; legacy routes each ray separately to a common outlet (same visible result, separate budgets). Confirm per-ray routing is the required model, under S485.
2. Is coupling loss 0.1 fixed (legacy) or authorable?
3. Confirm CAT-006 alone satisfies EL-213.
