# Rotary transmission and source shafts

Shafts, gearboxes, clutches, belts, motors and windmills are declaration data over the rigid-body, hinge/slider-constraint and drive capabilities of the generic WASM SIMD128 f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory)). Each ratio and engagement row is one generalized relation between two typed coordinates (revolute or prismatic, any combination), declared Bilateral or NonPositiveVelocity, and is solved in the shared Box2D v3 TGS Soft constraint pass; no part owns a solver. Delivered as ELEMENT-n roadmap slices (Reverse transmission CAT-057, Clutch CAT-018, Motor CAT-042, Conveyor CAT-019, Windmill CAT-070, Wound spring CAT-071).

## Declaration data

- **Shaft:** finite-inertia body with a local-Z frame, physical hull and anchored hinge; clockwise-positive observed speed/angle. Wheel art follows the committed body.
- **Reverse gearbox:** two shafts and a −1 phase-free ratio row.
- **Clutch:** two shafts, a +1 phase-free ratio row and an enum-typed Open/Engaged state; close_seconds is a real parameter from 0.05 to 2 seconds. Open contributes no velocity or acceleration coupling while hinges and identities remain; closing engages only at full coil closure; supply loss opens the coupling immediately while the plate animation finishes on its own.
- **Rigid shaft connection:** explicitly bound revolute coordinates carry the authored signed relation under whole-part rotation. Optional declared rim frames provide navy strands and gold committed-travel witnesses through the Animation worker; their radii/route never infer a physical ratio. Missing cosmetic descriptors leave the physical link valid. EL-200 friction/tension Belt and EL-201 Chain remain later contracts; current shaft artwork does not implement them.
- **Motor:** finite rotor mass/inertia, collision hull, anchored hinge and a [bounded drive](bounded-acceleration-drives.md) bound to its supply.
- **Windmill:** finite-inertia rotor on an X-axis hinge, authored hub/blade collision and output connections; signed response and calm-air loss come from [conserved rotary airflow](finite-gas-foundation.md#rotary-capture). The mass model is authored, not inferred from meshes; overlapping construction is corrected, not exempted from collision.

Admission rejects undefined engagement, missing/foreign/duplicate ownership and invalid parameter mappings atomically at compile.

## Behaviour for the player

- Coupled shafts share motion through the ratio row; a clutch closure cannot create energy (equal-inertia closure shares speed without gain); opening preserves existing output momentum, and a later input impulse cannot affect an uncoupled output.
- A motor supplies bounded effort and work; supply loss adds none and keeps an ideal hinge's momentum. Reported speed, travel events and artwork derive from committed physical motion, never from inferred engagement, copied downstream speed or independent angle integration.
- Low torque against inertia accelerates slowly; energy never exceeds supplied work ([envelope](gpu-f32-physics.md#game-grade-envelope)).

## Committed transmission reaction

Each generic ratio row uses one merged-body linear/angular Jacobian over its two coordinates and retains its accumulated reaction impulse. The 208-byte physics ABI record stores that f32 multiplier at offset 52, conjugate to the driven coordinate (N·s for a prismatic, N·m·s for a revolute driven coordinate); the driving, driven and constraint kinds follow at 56/60/64, four reserved zero bytes at 68, the compressive retention block at 72, signed interface work and positive transfer at 120/124, compression at 128, and the open accepted-work window carried across the tick commit from 132 (relation impulse; the driven coordinate's two bodies' window-start velocities; a retained moving body's window contact impulses, zero when unretained), refused when non-finite. Admission/Run/Reset initialize zero and an empty window at the admitted velocities; state admission rejects nonfinite reaction, nonzero reaction while disengaged and positive reaction on a NonPositiveVelocity row. Every candidate is compared with its committed source: the immutable declaration prefix, the coordinate/constraint kinds, and the retention identity and initial coordinate must be unchanged, and positive winding transfer, accepted elastic energy and released elastic energy must not decrease; any difference refuses the candidate.

Every tick reloads the committed impulse, including after candidate cancellation or engine recreation. Each 480 Hz substep prepares current frames/mass, clears reaction if disengaged or unable to act, and warm-starts once before the ordinary sweeps. Relaxation only recomputes frames and accumulates its deltas; every substep then writes its reaction, interface work and retention state to the candidate record. Pause does not apply it; Resume retains it. Cadence changes are admitted only in Building and reinitialize the state; attempts while Paused refuse without changing the reaction. All 60/120/240 Hz commits use the same 480 Hz physical step, so no impulse rescaling rule is needed. Ratio/inertia authoring changes require recompile/reset. There is no phase-lock term or alternate transmission law.

## Chrome-observable acceptance (post-catalogue)

- Reverse, open and engaged modes in axis-aligned and rotated constructions; impulse transfer versus a disconnected control; source-loss control; signed wind and calm-air dissipation for the windmill.
- Solved pose/speed match the presentation; exact Reset and Save/Load of rotated constructions after every Run; same construction and inputs give the same outcome.
- Reviewer grep for `clutch`, `gearbox`, or `windmill` in the physics solver finds nothing element-keyed.
