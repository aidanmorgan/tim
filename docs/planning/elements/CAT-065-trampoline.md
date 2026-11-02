# CAT-065 · trampoline declaration readiness spec

**Current delivery (24 October 2026):** [CAT-065 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 CAT-065-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. This page records the declaration knowledge the legacy holds; it does not restate the requirement row. Visual authority is the [Passive trampoline](../../../DESIGN.md#passive-trampoline) section of DESIGN.md.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-065 |
| Kind | `trampoline`, catalogue title "Trampoline", category "Motion" (`parts/catalog/trampoline.tres@a6c914e:L6-L8`) |
| Requirement anchor | [CAT-065](../requirements.md#current-cat-065); retained behaviour [sequence-task-306](../requirements.md#sequence-task-306) |
| Mapped identities | None. No EL/TH/RAD/GAP names a trampoline or membrane. [EL-194 Springboard](../invest/named-elements.md#element-194) (CAT-062) and [EL-215 Damped cushion](../invest/named-elements.md#element-215) are separate elements. Batch A confirms. |
| Roadmap story | [Story 10.5](../../../_bmad-output/planning-artifacts/epics.md) Trampoline Compliant Membrane Dynamics (CAT-065) Current [CAT-065 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Harvest-baseline status | Not started. `WorkshopPartKind` has no trampoline member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
**Shipped (Story 10.5, `elements/Trampoline/trampoline.tscn`).** One static frame body built as DESIGN.md's passive trampoline; every visible frame part collides (owner's "visible = solid" rule, 25 Oct 2026). In metres, in the part frame:
- navy back plate at (0, −0.51, 0), size 2.72 × 0.12 × 2.12. Its top face is the stroke floor −0.45 = rest height − maximum stroke, so it stops the stroke;
- two cream side rails at (±1.28, 0.1, 0), size 0.16 × 0.2 × 2.12, and two cream end rails at (0, 0.1, ±0.98), size 2.4 × 0.2 × 0.16. Their tops are the rest plane 0.2 and their inner faces the footprint edges x = ±1.2 and z = ±0.9;
- four gold corner posts, cylinders of radius 1/16 m (0.0625) and 0.45 tall, at (±1.28, −0.225, ±0.98): each stands on the plate's top face and reaches the rails' underside, outside the footprint (inner extents 1.2175 > 1.2 and 0.9175 > 0.9), so no post intrudes into the stroke volume. The radius is 1/16 m, the engine's minimum cylinder radius, drawn and colliding identically; it supersedes the harvest's and the loop-2 amendment's 0.055 m, which is below that minimum (owner, visible = solid);
- four gold feet, cylinders of radius 0.07 and height 0.2 at (±1.2, −0.67, ±0.9), below the plate;
- the sides stay open between the rails (underside at 0) and the plate (top at −0.45), so the compression reads from the side;
- **membrane (no collider):** a finite rectangular compliant footprint, half extents 1.2 × 0.9 m (bed 2.4 × 1.8 m), rest height 0.2 m, maximum stroke 0.65 m along the frame's local +Y normal, declared as `CompliantSurfaceDescriptor` and compiled to `CompliantSurfaceDeclaration`.

**Harvest baseline.** The legacy root body is the membrane frame; every legacy box is a collider (`AddBox` registers a box proxy with half extents = size/2, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Boxes, in metres, centred in the part frame (`parts/TrampolinePart.cs@a6c914e:L182-L186`):
  - navy back plate at (0, −0.51, 0), size 2.56 × 0.12 × 1.96;
  - two cream side rails at (±1.28, 0.1, 0), size 0.16 × 0.2 × 2.12;
  - two cream end rails at (0, 0.1, ±0.98), size 2.4 × 0.2 × 0.16;
  - membrane footprint as shipped (`parts/TrampolinePart.cs@a6c914e:L17-L19`, `parts/TrampolinePart.cs@a6c914e:L63-L66`);
  - gold corner supports: four cylinders, radius 0.055 m, height 0.6 m, at (±1.15, −0.2, ±0.85) (`parts/TrampolinePart.cs@a6c914e:L187-L189`), art only in the harvest. They stood inside the stroke volume, where a corner drop would hit a post before the bed, so the shipped posts stand at the rail corners instead.
- Harvest-time types: static `RigidBodyDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`) and box `ColliderDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`) cover the frame. The membrane footprint needs a new compliant-surface declaration (Story 10.5).

### Mass and material
- Frame: static, no mass.
- Rim and back material: restitution 0, bounce threshold 0.1 m/s, friction 0.3 (`parts/TrampolinePart.cs@a6c914e:L52-L52`; argument order from `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`). "The rigid rim/back absorb; only the membrane stores energy." Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`), with rolling resistance 0, the current value for non-sphere bodies (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`).
- Membrane: massless spring approximation, not cloth (`parts/TrampolinePart.cs@a6c914e:L11-L11`, `parts/TrampolinePart.cs@a6c914e:L96-L96`).

### Constraints and joints
- None on the frame. The membrane is one generic law driven by the declared surface data: an external force applied once per 480 Hz substep (not a joint or TGS constraint row, and with no element-, shape- or orientation-specific branch). Each body meets the bed at four support samples. See Work and energy stores.

### Typed sockets and ports
- None. The part is passive: "it needs no motor or trigger" (`tools/Campaign/TrampolineLesson.cs@a6c914e:L43-L43`). `WorkshopPorts.For` (`engine/gpu/WorkshopConnections.cs@a6c914e:L16-L26`) must return an empty port list, as it does for Ramp and Wall.

### Sensors and activation
- No activation output. The legacy raised a `Bounced` event when a body entered the membrane with approach speed ≥ 0.45 m/s (`parts/TrampolinePart.cs@a6c914e:L81-L92`). This was an observation and counter, not a socket.

### Work and energy stores
- **Elastic store.** Harvest: each engaged body stores 0.5·k·d², where k is `tension` and d is the compression depth (`parts/TrampolinePart.cs@a6c914e:L51-L51`). Shipped: Σ 0.5·(k/4)·(dᵢ − oᵢ)² over a body's bearing samples, with oᵢ each sample's offset (below).
- **Force law (shipped, four samples with offsets).** Each body meets the bed at its support points in the four diagonal directions −n ± ε·right ± ε·forward (ε = 1/64), found by the generic collider support query, so an axis-aligned box presents all four bottom corners, a flat face its extreme rim points and a sphere four points next to its lowest point. A sample *bears* the bed when it is `OnTop` (below), over the bed and below the rest plane, a purely geometric condition; it then carries a quarter of the bed: Fᵢ = max(0, (k/4)·(dᵢ − oᵢ) − (c/4)·vᵢ) along the frame normal, with dᵢ = clamp(rest − heightᵢ, 0, stroke), oᵢ its committed offset, vᵢ the normal velocity at the sample and c = 2·ζ·√(k·m). It never pulls. Legacy: the interval-mean spring force minus 2·ζ·√(k / m_eff)·speed (`engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66`).
- **Stroke.** dᵢ is clamped to the stroke, so beyond it the spring force saturates at the constant push (k/4)·(stroke − oᵢ), never more; the energy oracle counts the matching linear potential beyond the stroke. The shipped frame's restitution-0 back plate sits at the stroke floor, so a load reaches the saturated region only by pressing into the plate. The legacy constant-force-beyond-stroke potential (`engine/physics/CompressionSpringPotential.cs@a6c914e:L5-L24`) has the same shape and is carried as this clamp.
- **Contact state (shipped, per sample; Story 10.5 review loop 4).** Each (surface, dynamic body, sample direction) has one committed typed state `Outside`/`OnTop`/`Under` and one f32 offset oᵢ in the world memory; Run and Reset zero them (every sample `Outside`, which classifies itself on the first update), and no save field carries them. A sample is *over the bed* when it lies within the half-extents plus the footprint edge e on both in-plane axes; dᵢ = clamp(rest − heightᵢ, 0, stroke). An entering sample is classified on its unclamped depth max(0, rest − heightᵢ), so a deep entry is `Under` whatever the stroke; the force and the stored offset use dᵢ. The state follows each substep's resulting pose, and the next substep's force reads it at that pose:

  | From | Condition | To, offset |
  | --- | --- | --- |
  | any | not over the bed | `Outside`, oᵢ = 0 |
  | `Outside` | over, max(0, rest − heightᵢ) ≤ a | `OnTop`, oᵢ = dᵢ |
  | `Outside` | over, max(0, rest − heightᵢ) > a | `Under`, oᵢ = 0 |
  | `Under` | over, dᵢ = 0 (rose to the plane) | `OnTop`, oᵢ = 0 |
  | `Under` | over, dᵢ > 0 | `Under` |
  | `OnTop` | over | `OnTop`, oᵢ = min(oᵢ, dᵢ) |

  The committed deflection is the deepest dᵢ among `OnTop` over-bed samples, the visible indentation. The rule keeps three invariants:
  1. A sample that reaches the footprint on top bears the bed while it stays over the bed. On top means from above the plane, or crossing the edge no deeper than a.
  2. A sample that enters the footprint deeper than a is never pushed until it rises to the plane.
  3. The bed never returns more energy than it took: a sample starts bearing with its offset at its entry depth and the offset only shrinks, so re-entry of a tucked or rail-penetrating sample, a flipped-in sample and the crossing all cost no energy. Caveat: a support-direction jump between body features (at most about ε × the body's width, see below) can carry a sample's offset to a deeper point and so store up to that much unpaid compression.
- **Tolerances.**
  - **Arrival depth a, declared per surface (owner, 2 November 2026).** Each compliant surface declares its arrival depth (`CompliantSurfaceDescriptor.ArrivalDepth`, m); the engine holds no package-sized tolerance. It travels with the other surface fields through the bundle payload, the compiled declaration, the ABI surface row (offset 12) and the compiled-scene wire record, and every boundary that refuses the other fields refuses a value that is not finite, positive and under half the stroke. A frame that declares a surface must leave a side opening (rest plane to the underside of whatever frames the footprint) of at least 2a, so a body entering under the frame is always `Under`.
    - **The Trampoline declares a = 0.10 m.** At tilt all four diagonal samples sit on a body's lowest end, so a hidden end becomes a sample only when the support flips to it. The deepest such flip-in measured over the shipped shapes and entries (straddles at 8.2 and 12 m/s, rail-first, tilted 20°, fast slides; the Weight from 0.25 to 8 kg) is 0.046 m at 300 N/m, under both the attempt-4 rule and the shipped one (a 1.1 m Domino tipping in from a rail). a gives about twice that, and the frame's side opening (the rail underside is 0.2 m below the plane) is 2a. With offsets it admits no unpaid energy. Rail-top penetration does not size it: a load landing on a rail top at 12 m/s sinks 0.2–0.3 mm. a must be re-derived if the frame, the sample law or the catalogue's widest body changes.
  - **Footprint edge e = 1 cm (engine constant).** A flush-contact tolerance, independent of any frame's dimensions: it covers a load pressed flush against a frame face (its edge samples sit a fraction of a millimetre past the footprint). It is not widened, so a sample on a rail top beyond the footprint edge does not count as over the bed.
- **Known costs and limits.**
  - **Sag bias.** A load that never leaves the bed keeps the offsets it entered with: its resting sag lies in [m·g/k, m·g/k + max oᵢ]. A load that falls onto the open bed has every sample over the bed and `OnTop` from above the plane, so its offsets are 0 and its sag m·g/k; offsets arise only for samples entering below the plane (edge crossings, flip-ins), at most a. Bouncing loads reset on each re-entry above the plane.
  - **Support-direction identity.** A direction's support can jump between body features, by about ε × body width (about 1.7 cm for the 1.1 m Domino); the state inherits the direction. At tilt the samples collapse onto the lowest end, so a body whose lowest end is past the footprint edge is not borne even while most of it is over the bed (seen as a body leaving under a rail loses the bed when its lowest rim crosses the edge).
  - **Under-rail exit.** A body pressed below the rail underside next to a rail can slide out under the rail to the plate, with no energy gain, only if its vertical extent is under the 0.45 m opening: an upright Weight up to about 1 kg, a lying Weight up to about 0.6 kg and a lying Domino can; the Basketball (0.68 m) and the default 4 kg Weight cannot.
  - **No tangential friction (residual, deferred by the owner on 2 November 2026).** Compliant surfaces have no tangential friction or rolling resistance: the membrane pushes only along its normal, so a load on the bed slides freely along it until a rail stops it (a Weight thrown in by a 12 m/s straddle still drifts at 0.4 m/s after 8 s). The intended law is per bearing sample and energy-removing only, and needs its own edge-case analysis; it is tracked in `deferred-work.md`.
- **Clear region.** The shipped Basketball (r 0.34 m) dropped from 7 m clears the rails when it lands with |x| ≤ HalfX − r = 0.86 m and |z| ≤ HalfZ − r = 0.56 m. Outside it the ball meets a rail's inner top edge as it sinks: the rigid, inelastic rail edge deflects it away from the rail across the bed with no energy gain, and it is never thrown sideways along the rail or pinned against it, and while over the frame its speed across the rails stays within half its arrival speed. Where it finally comes to rest is not part of the contract.
- **Committed read.** `CompliantSurfaceRead(Frame, Deflection)` carries each surface's deepest `OnTop` over-bed sample depth (0 at rest) on the response wire; the bed visual binds it.
- **Stability envelope.** The membrane is an explicit per-substep spring and damper. Each bearing point acts on its body's effective mass at that point, m_eff = 1/(1/m + (r×n)·I⁻¹·(r×n)) with r the point's offset from the centre of mass, and the update stays stable while ω_eff·h < 2(√(1 + ζ²) − ζ), with ω_eff = √((k/4)·n_coincident / m_eff) and n_coincident the number of samples acting together at that point. The bound counts the explicit damper (ζ) and the off-centre coupling. At the worst corner, k = 1200 N/m, ζ = 0.8 and h = 1/480 s: 2(√1.64 − 0.8) ≈ 0.961, so ω_eff < 461 rad/s and m_eff > 300·n_coincident / 461² ≈ 1.4 g·n_coincident.
  - A single point at the centre of mass (m_eff = m) is stable above about 1.4 g.
  - Four-corner rocking of a flat box with the Domino's proportions has m_eff ≈ m/6.5 at each corner (I from 0.55 × 0.125 × 0.325 m half extents). With the four samples acting together (n_coincident = 4) the floor is about 37 g.
  - The catalogue's lightest dynamic bodies clear it by far: the 0.25 kg Weight and the 0.4 kg Domino, even counting the Domino's rocking at its ends. A future light body (for example EL-208 Balloon) must be checked against this bound.
- **Binding.** Every dynamic body owned by another part is a potential load. A surface frame may be declared only once (`engine/SceneCompliantSurface.cs@a6c914e:L25-L50`).
- **Preload.** The legacy law supports a `Preloaded` initial state; the trampoline uses `Unloaded` (`engine/physics/CompliantContactState.cs@a6c914e:L6-L6`, `parts/TrampolinePart.cs@a6c914e:L63-L66`).
- Nearest current precedent: `ContactWorkDeclaration` (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L37`) is a paid or passive finite reservoir. It is not a compliant law. A new declaration is required.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `tension` | f32 | 120–1200 | 300 (harvest 180) | N/m (used as contact-spring stiffness k) | `parts/TrampolinePart.cs@a6c914e:L54-L61`, `parts/catalog/trampoline.tres@a6c914e:L12-L12` |
| `damping_ratio` | f32 | 0.08–0.8 | 0.12 | dimensionless ζ | same |

The shipped default tension is 300 N/m. At the harvest's 180 N/m the shipped 1 kg Basketball dropped from the lesson's 7 m (8.2 m/s at the bed) would compress about 0.67 m undamped, beyond the 0.65 m stroke, and bottom out on the back plate; at 300 N/m it peaks near 0.51 m and stays on the membrane, and the defaults deliver the lesson capture.

The parameters are declared by key (`tension`, `damping_ratio`) in the package metadata and bound to the surface through `MechanicalValue.FromParameter`; placed instances carry them as `ParameterId`-keyed values, which the compiler evaluates into the declaration. The legacy closed enum `TrampolineParameter { Tension, DampingRatio }` (`parts/TrampolinePart.cs@a6c914e:L9-L9`) is not carried.

### Cosmetic curves and UI bindings
- **Membrane skin.** A 24 × 18 grid mesh over the footprint (`parts/TrampolinePart.cs@a6c914e:L143-L168`). Its height field is driven only by committed contact patches (`parts/TrampolinePart.cs@a6c914e:L96-L141`):
  - per patch, the dent falls off as (1 − f²)², where f is the distance over the support reach to the fixed frame, measured along the ray from the patch (direction-aware sag);
  - the skin stays below a sphere of the patch's rounding radius;
  - a flat supporting feature gets a flat lower bound under its footprint;
  - neighbouring patches limit each other's reach;
  - frame edges stay at the rest height.
- No cosmetic curve uses `CosmeticCurveDeclaration` (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L16`). The skin is a continuous deformation binding to committed contact state, which is a new binding kind.
- **Shipped (Story 10.5).** `Visual/Visual_Bed` binds `PresentationDriver.CommittedSurfaceDeflection` on its local Y: flat at its 0.19 m rest height (top face on the rest plane) at rest and after Reset, and lowered by (0.65 − 0.001)/0.65 m per metre of committed deflection, so at full stroke its top face stays 1 mm above the back plate's top face and the faces never coincide. It therefore reads at most 1 mm shallower than the deepest contact, invisible beside the contacting body (DESIGN.md: the profile stays below the contacting sphere to within that millimetre). The direction-aware grid skin above is deferred to a later presentation refinement (`deferred-work.md`).
- Pick radius 1.8 (harvest 1.5, `parts/TrampolinePart.cs@a6c914e:L181-L181`): the shipped frame is 2.72 m wide.

### Art
- Scene `parts/scenes/trampoline.tscn` is a bare `Node3D` with the script (`parts/scenes/trampoline.tscn@a6c914e:L1-L4`); all geometry is procedural.
- Palette: navy back `#293954`, cream frame `#fff8e9`, gold supports `#e8b764`, cyan membrane `#66b8c9` with back-face culling disabled (`parts/TrampolinePart.cs@a6c914e:L182-L191`). Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/trampoline.tres@a6c914e:L11-L11`).
- The toolbox pictogram shows a ball above a sagging bed and must not reuse the springboard icon ([DESIGN.md](../../../DESIGN.md#passive-trampoline)).

### Catalogue and inventory entry
- Description: "Drop a ball onto the cyan bed. Its spring tension returns part of the impact energy, without a powered launch. Tilt it to redirect the rebound. The cream rim and navy back do not bounce. Uses bounded contact springs, not full cloth physics." (`parts/catalog/trampoline.tres@a6c914e:L9-L9`).
- Inventory: one trampoline in `a_gentle_rebound` (`content/puzzles.json@a6c914e:L32571-L32573`). Declare the package with a counted allowance keyed by its bundle-issued typed id (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

Capability families: the [CAT-065 row](../general-engine-element-map.md) and its binding in [catalogue-elements.json](../../coverage/catalogue-elements.json). They are not repeated here.

**Exists now**
- Static frame bodies and box colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- Rim and back contact material: `ContactMaterialDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`.
- Rigid contact solve: Box2D v3 TGS Soft at 480 Hz substeps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L208`), 8 biased and 4 relax iterations (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`), soft contact stiffness `makeSoft` (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`), normal rows (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774-L791`), manifold sweeps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`) and restitution (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L888-L900`).
- Dynamic sphere payload mass properties: `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L51`.
- Rotated placement: `CanonicalRotation`, `engine/gpu/WorkshopConstruction.cs@a6c914e:L15-L36`.

**Missing**
- Compliant membrane law: a unilateral spring-damper over a finite rectangular footprint, with per-body engagement history and stroke. The force is applied once per solver step, not per projection iteration. Builds in **Story 10.5**. The generic elastic spring constraint of **Story 6.4** also "serves Trampoline CAT-065" ([springboard contract](../../springboard-elastic-contract.md)), but the trampoline's massless contact patches remain a different declaration.
- A committed contact-patch read for the skin binding and the impact count: **Story 10.5**, on the Epic 4 animation worker.
- Package-owned Trampoline declaration, bundle catalogue metadata and authored inventory allowance: **Story 10.5**.
- Tether integration needs ropes: **Story 10.2** (CAT-058 Rope anchor, CAT-067 Weight).
- Drop→trampoline→pipe teaching needs the pipe: **Stories 6.6/6.7** (CAT-048).

**Element dependencies.** Payload CAT-001 Basketball. Lesson goal CAT-004 Receiver. Tether controls CAT-058 and CAT-067. Pipe route CAT-048.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Bed half extents 1.2 × 0.9 m, rest height 0.2 m, maximum stroke 0.65 m | `parts/TrampolinePart.cs@a6c914e:L17-L19` | carry forward | Authored geometry; supported by the requirement's finite footprint |
| 2 | Frame boxes, back plate at the stroke limit, rails flush with the footprint | `parts/TrampolinePart.cs@a6c914e:L182-L186` | carry forward | Rigid rim/back geometry |
| 3 | Rim/back material restitution 0, threshold 0.1 m/s, friction 0.3 | `parts/TrampolinePart.cs@a6c914e:L52-L52` | carry forward | Rim/back absorb; only the membrane stores energy |
| 4 | Tension 120–1200, default 180; damping ratio 0.08–0.8, default 0.12; NaN and out-of-range values reject | `parts/TrampolinePart.cs@a6c914e:L54-L61`, `parts/catalog/trampoline.tres@a6c914e:L12-L12`, `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L300-L315` | replaced | Ranges and NaN/out-of-range rejection carried, stored as f32; the default tension is replaced by 300 N/m (see Parameters), so the lesson ball stays on the membrane |
| 5 | Unilateral force = interval-mean spring force − 2ζ√(k/m_eff)·speed, never negative | `engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66` | carry forward (law) / do not carry forward (CPU double interval evaluator) | The law is admissible; the legacy CPU `AccelerationSolver` path is not |
| 6 | Constant force beyond the stroke | `engine/physics/CompressionSpringPotential.cs@a6c914e:L5-L24` | carry forward | Shipped as the stroke clamp: the force saturates at (k/4)·(stroke − oᵢ) and the back plate at the stroke floor stops travel (§2 Stroke) |
| 7 | Engagement phases Unarmed/Ready/Engaged; entry from above while moving down; leaving the footprint disarms | `engine/physics/CompliantContactState.cs@a6c914e:L6-L68` | replaced | Whole-body phases answered a per-point question with a body-wide conjunction and dropped legitimate entries (a Domino seesawing on a rail never re-armed); per-sample `Outside`/`OnTop`/`Under` states with offsets decide geometry, and offsets carry energy honesty (§2 Contact state, Story 10.5 review loop 4) |
| 8 | Every other owner's dynamic body is a potential load; a duplicate frame rejects; an invalid law rejects at Run | `engine/SceneCompliantSurface.cs@a6c914e:L13-L50`, `CuriousContraptions.tests/CompliantSurfaceBindingTests.cs@a6c914e:L46-L61` | carry forward | Atomic rejection |
| 9 | An impact counts only at approach speed ≥ 0.45 m/s | `parts/TrampolinePart.cs@a6c914e:L81-L92` | carry forward as an observation only | No socket; see Open questions |
| 10 | Stored energy 0.5·k·d² summed over patches | `parts/TrampolinePart.cs@a6c914e:L51-L51` | carry forward | Energy ledger term |
| 11 | Patch observation and the `ObservePhysics` per-part callback | `parts/TrampolinePart.cs@a6c914e:L68-L94` | do not carry forward | Per-element update loop; the worker owns the state |
| 12 | Direction-aware skin height field, below-sphere guarantee, fixed edges | `parts/TrampolinePart.cs@a6c914e:L96-L141`, `CuriousContraptions.tests/TrampolineSkinTests.cs@a6c914e:L82-L135` | carry forward | DESIGN requires direction-aware sag bounded by the frame |
| 13 | Single impact; peak compression 0.03–0.65 m; peak energy ≤ 1.05 × incoming; rebound speed 0.25–0.95 × incoming; no lateral velocity, for (mass, speed, tension) = (0.5, 6, 180), (1, 6, 180), (4, 2, 180), (1, 6, 600), (1, 2, 120) | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L34-L80` | carry forward (behaviour) | The 1.05 allowance is a legacy integration bound; freeze an f32 bound |
| 14 | The rebound follows the rotated normal for (0,0,±30), (90,0,0), (0,90,0), (180,0,0); from 4 m/s it leaves at 1–3.9 m/s along the normal, tangential ≤ 0.01 | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L82-L103` | carry forward | Tilted and inverted mounting |
| 15 | Resting mass 1 and 4 settle with no impact and compression ≈ m·g/180 (±0.015 m), speed ≤ 0.03 m/s | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L105-L126` | carry forward | No repeated launches |
| 16 | Rim hit, outside the bed, from the back, and grazing produce no contact, no compression and no speed gain | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L128-L146` | carry forward | Rim/back/miss controls |
| 17 | Tension 120 gives > 1.8× the peak compression and contact time of 600 | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L148-L169` | carry forward | Tension changes compression/contact duration |
| 18 | Two concurrent balls (1 kg and 2 kg at ±0.4 m) give the same result in either insertion order; 2 impacts | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L196-L217` | carry forward | Simultaneous loads; order independence |
| 19 | Mass 4 and 8 at 12 m/s bottom out at a peak ≥ 0.60 m, never beyond 0.65, with no added energy | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L219-L240` | carry forward | Overload control |
| 20 | Reset mid-contact after 2, 12 or 24 ticks restores the unloaded skin; the replay is identical | `CuriousContraptions.tests/TrampolineTests.cs@a6c914e:L242-L267` | carry forward | Run/Reset |
| 21 | A centred two-ball stack settles at compression ≈ (m1 + m2)·g/180 ±0.015, centred within 0.001 m, energy ≤ 1.01 × initial, either insertion order | `CuriousContraptions.tests/TrampolineStackTests.cs@a6c914e:L31-L89` | replaced | Stacked loads: a ball stack holds only by symmetry, so Story 10.5 (review loop 5) proves the stacked-load compression with a 1 kg Weight on a 4 kg Weight |
| 22 | A taut rope tether holding a weight above the bed prevents contact; a slack tether (+2.3 m) allows rebound; rope length ≤ L + 0.002; deterministic replay | `CuriousContraptions.tests/TrampolineRopeTests.cs@a6c914e:L33-L118` | carry forward | Tether integration |
| 23 | An aimed bed (−30° about Z at (0,3,0)) sends the ball through a pipe at (3, 3.1, 0); a flat bed or a pipe moved to z = 2 does not | `CuriousContraptions.tests/TrampolinePipeTests.cs@a6c914e:L31-L92` | carry forward | Drop→trampoline→pipe teaching row |
| 24 | In non-strict precision the placement-assisted bed moves kinematically and impacts using relative speed; strict precision keeps it static | `CuriousContraptions.tests/TrampolineSkinTests.cs@a6c914e:L12-L74` | do not carry forward as settled | Legacy prescribed-motion assistance; see Open questions |
| 25 | Hidden rendering still bounces; a disabled payload or mechanism collider gives no impact and keeps −6 m/s | `CuriousContraptions.tests/ContactParticipationTests.cs@a6c914e:L20-L75` | carry forward | Behaviour follows physics, not presentation |
| 26 | Engagement needs no scene preparation or observation; rendering cannot feed back into motion | `CuriousContraptions.tests/TrampolineOwnershipTests.cs@a6c914e:L19-L62` | carry forward | One-way data flow |
| 27 | A failed tick restores every patch, the skin and the counters | `CuriousContraptions.tests/TrampolineRuntimeCheckpointTests.cs@a6c914e:L30-L92`, `CuriousContraptions.tests/CompliantSurfaceBindingTests.cs@a6c914e:L63-L106` | carry forward; deferred to the injected-fault gate | The rollback outcome is retained, not the legacy transaction mechanism; proof waits for the injected-fault gate |
| 28 | Lesson "A gentle rebound": ball locked at (0,7,0), basket at (5.4,1.2,0), solution bed at (0,3,0) rotated −30° about Z, goal Captured; hint text; assistance windows 0.2/0.1 m and 5°/2° | `tools/Campaign/TrampolineLesson.cs@a6c914e:L20-L52`, `content/puzzles.json@a6c914e:L32565-L32775` | carry forward | Introductory fixture; rebuild authoring in Epic 15 |
| 29 | Lesson outcomes: at precisions 0, 0.45 and 1 the aimed bed wins; a flat bed impacts but misses; depth 2 has no impact; a missing bed fails | `CuriousContraptions.tests/TrampolineCampaignTests.cs@a6c914e:L15-L59` | carry forward | Positive and negative controls |
| 30 | Lesson appended after the wound-spring lessons | `tools/Campaign/Program.cs@a6c914e:L575-L579` | carry forward (order fact) | Final teaching order is an Epic 15 decision |
| 31 | Skin neighbour reach: another patch limits a patch's support radius to 0.49 × their separation (only when that exceeds the rounding radius); between the rounding radius and the support the skin follows the cubic tail (1 − t)²(1 + 2t) of the depth beyond the rounding radius | `parts/TrampolinePart.cs@a6c914e:L113-L132` | carry forward | Cosmetic deformation bound; follows committed patches only |
| 32 | Impacts are counted at most once per physics step: the step index guard processes each step's compliant entries once | `parts/TrampolinePart.cs@a6c914e:L81-L92` | carry forward (observation rule) | No double counting across repeated observation |
| 33 | Engagement requires the whole lowest-support footprint strictly inside the bed half-extents (min > −half and max < half on X and Z); a body outside it gives no force and disarms | `engine/physics/SupportFootprint.cs@a6c914e:L11-L12`; `engine/physics/CompliantContactLoad.cs@a6c914e:L50`; `engine/physics/CompliantContactState.cs@a6c914e:L43`, `engine/physics/CompliantContactState.cs@a6c914e:L59` | replaced | Whole-footprint arming dropped flat loads that slid or tipped in over a rail; over-bed samples carry the bed and the frame carries the rest (§2 Contact state, Story 10.5 review loops 3 and 4) |

### Files harvested
- `parts/TrampolinePart.cs`, `parts/catalog/trampoline.tres`, `parts/scenes/trampoline.tscn`
- `engine/SceneCompliantSurface.cs`, `engine/physics/CompliantContactLoad.cs`, `engine/physics/CompliantContactState.cs`, `engine/physics/SupportFootprint.cs`, `engine/physics/CompressionSpringPotential.cs`, `engine/physics/PersistentContactPair.cs` (material argument order only)
- `engine/bridge/CommittedPoseBuffer.Compliant.cs` (checked, no element knowledge: generic committed contact staging)
- `engine/physics/PhysicsWorld.cs` L135-L145, L340-L354 (checked, no element knowledge beyond row 7)
- `reference/cpu/MachinePart.cs` (`AddBox` collider semantics)
- `CuriousContraptions.tests/TrampolineTests.cs`, `TrampolineStackTests.cs`, `TrampolineRopeTests.cs`, `TrampolinePipeTests.cs`, `TrampolineSkinTests.cs`, `TrampolineCampaignTests.cs`, `TrampolineOwnershipTests.cs`, `TrampolineRuntimeCheckpointTests.cs`, `CompliantSurfaceBindingTests.cs`, `ContactParticipationTests.cs`
- `tools/Campaign/TrampolineLesson.cs`, `tools/Campaign/Program.cs`
- `content/puzzles.json` (`a_gentle_rebound`)
- `reference/p054-guide/puzzles-candidate.json` (checked, no element knowledge: duplicate of the `a_gentle_rebound` level)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file list only)

## 5. Acceptance outline

Acceptance criteria are those of the [CAT-065 row](../requirements.md#current-cat-065), [sequence-task-306](../requirements.md#sequence-task-306) and Story 10.5.
- **Chrome UI recipe.** Open `a_gentle_rebound`. Place one trampoline from the toolbox at (0, 3, 0) and rotate it −30° about Z with the gizmo. Run. The existing browser recipe `committed-compliant-receiver-v1` uses the same set-up (`docs/committed-compliant-recipes.json@a6c914e:L2-L41`).
- **Positive.** The ball compresses the membrane, rebounds along the tilted normal and is captured. Peak energy never exceeds the initial potential energy.
- **Negative/control.** With a flat bed, the ball impacts but misses. With the bed at depth z = 2, there is no contact; the recipe `committed-compliant-missed-v1` instead moves the ball to z = 2 (`docs/committed-compliant-recipes.json@a6c914e:L42-L81`). A missing bed fails. Rim, back and grazing hits store nothing.
- **Boundaries.** Tension 120 and 1200, damping 0.08 and 0.8, with values outside rejected atomically. Bottoming out at 0.65 m. Off-centre (±0.8 m), simultaneous, stacked and inverted loads.
- **Run/Reset.** Reset at any tick restores the unloaded skin and zero stored energy, and the replay is identical (rows 20, 21).
- **Save/Load.** Position, rotation, `tension` and `damping_ratio` round-trip through `WorkshopSaveCodec` (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Integrations.** Rope tether (taut and slack) after Story 10.2. Trampoline→pipe after Stories 6.6/6.7.

## 6. Open questions

1. **Tension semantics.** The legacy `tension` is a per-contact spring stiffness in N/m, not a membrane tension. Confirm the name and unit, or rename it under the closed-enum rule.
2. **Membrane model.** Resolved (Story 10.5 review loop 1): several samples per body. Each body meets the bed at four diagonal support samples carrying k/4 each, so flat payloads rest flat and boxes present all four corners.
3. **Bottoming out.** Resolved (Story 10.5 review loop 1; wording corrected in loop 5): the spring force saturates beyond the stroke at the constant push (k/4)·(stroke − oᵢ), the legacy constant-force shape, and the shipped frame's restitution-0 back plate at the stroke floor stops travel.
4. **Impact observation.** Should the 0.45 m/s `Bounced` threshold stay as a hint/teaching observation? The part has no socket.
5. **Placement assistance.** Can difficulty assistance move a placed trampoline kinematically during Run (row 24)? This conflicts with "no prescribed launch". Unspecified — owner decision.
6. **f32 bounds.** Freeze f32 game-grade bounds for the energy allowance (legacy 1.05 and 1.01), the settle tolerance (0.015 m) and the speed bounds.
7. **Tether order.** Resolved by the 9 Oct 2026 reorder: rope Story 10.2 now precedes trampoline Story 10.5, so the tether criterion no longer has to move.
8. **Level number.** The level subtitle says 61 (`content/puzzles.json@a6c914e:L32568-L32568`), but the browser recipe says level 63. Epic 15 sets the final index.
