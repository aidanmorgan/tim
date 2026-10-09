# EL-102 · Large-bore ball pipe — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-102 · Large-bore ball pipe · Specialist |
| Requirement anchor | [element-102](../requirements.md#element-102); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-102](../invest/named-elements.md#element-102); source owner **S362** |
| CAT spec refined or extended | Extends [CAT-048 Clear pipe](CAT-048-pipe.md): the same annular tube with a scaled bore |
| Related identities | EL-071 Straight metal ball pipe, EL-072 Curved metal ball pipe, EL-103 Accelerator tube, EL-080 Programmable box (larger cargo) |
| Roadmap story | Unscheduled |
| Status | Not started. The standard pipe's declaration is parked and not compiled (`CuriousContraptions.csproj@a6c914e:L29-L29` excludes `WorkshopPipe.cs` and `AnnularProfile.cs`). |

## 2. Declaration

The bore is a declared geometry value, and cargo admission is decided by real collision with that geometry and by typed mouth classes, never inferred from art (requirement row).

- **Bodies and shapes.** One static `AnnularProfile` tube along local X (field order HalfLength, InnerRadius, MiddleRadius, EndRadius, EndHalfWidth, `engine/gpu/AnnularProfile.cs@a6c914e:L22-L23`). All values are radii or half-lengths:

  | Field | Large bore | Standard pipe (for comparison) | Status |
  | --- | --- | --- | --- |
  | InnerRadius (bore) | 1.0 m | 0.65 m (`engine/gpu/WorkshopPipe.cs@a6c914e:L9-L9`) | **proposed**: 2.0 m diameter, so cargo up to about 1.9 m across passes; inside the 2 m admission bound (`engine/gpu/AnnularProfile.cs@a6c914e:L28-L28`) |
  | MiddleRadius (shell) | 1.08 m | 0.70 m (`engine/gpu/WorkshopPipe.cs@a6c914e:L17-L18`) | **proposed**: the standard shell-to-bore ratio, 0.70 ÷ 0.65 ≈ 1.08 |
  | EndRadius (collar) | 1.20 m | 0.78 m (same lines) | **proposed**: the standard ratio, 0.78 ÷ 0.65 = 1.20 |
  | EndHalfWidth (collar) | 0.14 m | 0.09 m (same lines) | **proposed**: the standard ratio, 0.09 ÷ 0.65 ≈ 0.14, under the 0.25 m bound (`engine/gpu/AnnularProfile.cs@a6c914e:L31-L31`) |
  | HalfLength | `length` ÷ 2 | `length` ÷ 2 | As the standard pipe |

- **Mass and material.** Static; restitution 1, threshold 0.1 m/s and friction 0.3, the static-surface convention (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None.
- **Typed ports (mouths).** Two typed mouths, at ±HalfLength, of `BoreClass.Large` (**proposed**: an enum {Standard, Large}). A mouth snaps only to a mouth of the same class; joining Large to Standard is refused at placement, with no adapter or alias (**proposed**: size is validated, not guessed).
- **Sensors and activation.** None.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `length` | f32 | 2–8 | 3.6 | m | The standard default 3.6 (`engine/gpu/WorkshopPipe.cs@a6c914e:L8-L8`); minimum **proposed** at 2 m because the profile needs HalfLength > EndHalfWidth with a visible shell |

- **Cosmetic curves and UI bindings.** None. Resizing works like the standard pipe: one local-axis handle, the centre stays put, Escape cancels, one drag is one Undo (`DESIGN.md@a6c914e:L299-L299`).
- **Art (DESIGN.md).** The clear pipe language at a larger scale: a transparent cyan shell (alpha 0.16), open cream collars and two thin navy rails (`DESIGN.md@a6c914e:L181-L181`, `DESIGN.md@a6c914e:L299-L299`). A thicker double collar ring marks the Large class without relying on colour.
- **Catalogue and inventory entry.** Id `pipe_large`, title "Large clear pipe", category Motion, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Annular profile admission and the standard pipe declaration are written but excluded from the build (`engine/gpu/AnnularProfile.cs@a6c914e:L22-L36`; `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L41`).
- **Missing.** The annular collider in the solver and mouth snapping: Stories 6.6/6.7 (CAT-048). The `BoreClass` mouth typing and a parameterised bore: owner S362.
- **Dependencies.** CAT-048 Pipe (Stories 6.6/6.7) first. Cargo: CAT-001 Basketball, EL-080 Programmable box.

## 4. Sources and legacy

- **Requirement row.** [element-102](../requirements.md#element-102): a scaled physical bore supports correspondingly larger cargo; too-large cargo remains blocked; size is validated rather than guessed from art.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "large bore", "bore" and "oversize" over every Epic 7 deletion path found no large-bore source. Standard pipe facts apply; [CAT-048](CAT-048-pipe.md) holds the full pipe harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Standard pipe passes a 0.34 m ball at 40 m/s; a 0.8 m ball at 4 m/s hits the mouth and stays upstream (x < −1.89) | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | Carry forward: the large bore must pass what the standard blocks, and still block cargo larger than itself |
| L2 | Only finite pipe length may change; the bore is fixed | `engine/gpu/WorkshopPipe.cs@a6c914e:L21-L27` (parked) | Carry forward: the large bore is a separate element, not a resize of the standard bore |
| L3 | The pipe adapter reads bore and collar radii from the profile and builds a 0.16-alpha cyan shell with cream collars and navy rails | `parts/PipePart.cs@a6c914e:L15-L30` | Carry forward the art construction at the larger scale. Do not carry forward the Godot adapter. |

**Files harvested:** `CuriousContraptions.tests/PipeTests.cs`, `parts/PipePart.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place a Large clear pipe tilted downhill and, beside it, a standard Clear pipe at the same tilt. Feed an EL-080 Programmable box (Medium) and a Basketball into each with the real palette. Try to snap a Large mouth to a standard mouth. Run.
- **Positive.** The Medium box (1.2 × 0.8 m cross-section, 1.44 m diagonal) slides through the large bore and out of the far mouth.
- **Negative or control.** The same box is blocked at the standard pipe's mouth. The Large-to-Standard join is refused and the construction is unchanged. A Large preset box presented with its 1.6 × 1.6 m face (2.26 m diagonal) is blocked at the large mouth.
- **Boundaries.** No cargo passes through the shell; rest penetration ≤ 0.5 mm; length 2 and 8 m accepted, 1.9 m refused.
- **Run/Reset.** Reset restores all cargo. **Save/Load.** Length, pose and mouth class round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Ball straight pipe, both bend angles, funnel" reserves levels 21–30 with "specialist routes 91–100" (reuse 41–50, 111–120, 136–150). Element integrations: Clear pipe (CAT-048) for the class-mismatch control, EL-080 Programmable box cargo, Basketball (CAT-001).

## 6. Open questions

1. Bore size: 2.0 m diameter (proposed) or another scale. Unspecified — owner decision (S362).
2. Whether Large bends (45° and 90°) are needed with it.
3. Which cargo element is the teaching example (the Programmable box is proposed; no catalogue ball needs a large bore).
