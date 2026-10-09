# Rotary transmission and source shafts

Shafts, gearboxes, clutches, belts, motors and windmills are declaration data over the rigid-body, hinge-constraint and drive capabilities of the generic WASM SIMD128 f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory)). The ratio and engagement rows are solved in the shared Box2D v3 TGS Soft constraint pass; no part owns a solver. Delivered as ELEMENT-n roadmap slices (Reverse transmission CAT-057, Clutch CAT-018, Motor CAT-042, Conveyor CAT-019, Windmill CAT-070, Wound spring CAT-071; register S184).

## Declaration data

- **Shaft:** finite-inertia body with a local-Z frame, physical hull and anchored hinge; clockwise-positive observed speed/angle. Wheel art follows the committed body.
- **Reverse gearbox:** two shafts and a −1 phase-free ratio row.
- **Clutch:** two shafts, a +1 phase-free ratio row and an enum-typed Open/Engaged state; CloseSeconds is an enum-typed parameter choice. Open contributes no velocity or acceleration coupling while hinges and identities remain; closing engages only at full coil closure; supply loss opens the coupling immediately while the plate animation finishes on its own.
- **Belt/chain:** a socket-to-hinge ratio row with its authored signed ratio, preserved under whole-part rotation. Wound-spring winding and conveyor consumers use the same transfer; no downstream adapter duplicates source work.
- **Motor:** finite rotor mass/inertia, collision hull, anchored hinge and a [bounded drive](bounded-acceleration-drives.md) bound to its supply.
- **Windmill:** finite-inertia rotor on an X-axis hinge, authored hub/blade collision and output connections; signed response and calm-air loss come from [conserved rotary airflow](finite-gas-foundation.md#rotary-capture). The mass model is authored, not inferred from meshes; overlapping construction is corrected, not exempted from collision.

Admission rejects undefined engagement, missing/foreign/duplicate ownership and invalid parameter mappings atomically at compile.

## Behaviour for the player

- Coupled shafts share motion through the ratio row; a clutch closure cannot create energy (equal-inertia closure shares speed without gain); opening preserves existing output momentum, and a later input impulse cannot affect an uncoupled output.
- A motor supplies bounded effort and work; supply loss adds none and keeps an ideal hinge's momentum. Reported speed, travel events and artwork derive from committed physical motion, never from inferred engagement, copied downstream speed or independent angle integration.
- Low torque against inertia accelerates slowly; energy never exceeds supplied work ([envelope](gpu-f32-physics.md#game-grade-envelope)).

## Chrome-observable acceptance

- Reverse, open and engaged modes in axis-aligned and rotated constructions; impulse transfer versus a disconnected control; source-loss control; signed wind and calm-air dissipation for the windmill.
- Solved pose/speed match the presentation; exact Reset and Save/Load of rotated constructions after every Run; same construction and inputs give the same outcome.
- Reviewer grep for `clutch`, `gearbox`, or `windmill` in the physics solver finds nothing element-keyed.
