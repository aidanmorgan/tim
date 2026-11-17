# Rotary transmission and source shafts

Shafts, gearboxes, clutches, belts, motors and windmills are declaration data over the rigid-body, hinge-constraint and drive capabilities of the generic WASM SIMD128 f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory)). The ratio and engagement rows are solved in the shared Box2D v3 TGS Soft constraint pass; no part owns a solver. Delivered as ELEMENT-n roadmap slices (Reverse transmission CAT-057, Clutch CAT-018, Motor CAT-042, Conveyor CAT-019, Windmill CAT-070, Wound spring CAT-071).

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

Each generic ratio row uses the existing scaled, merged-body angular Jacobian and retains its accumulated reaction impulse. The 64-byte physics ABI record stores that f32 multiplier at offset 52 (N·m·s); zero padding begins 56. Admission/Run/Reset initialize zero; state admission rejects nonfinite reaction and nonzero reaction while disengaged. The immutable declaration prefix and compiled FlatBuffer relation are unchanged.

Every tick reloads the committed impulse, including after candidate cancellation or engine recreation. Each 480 Hz substep prepares current frames/mass, clears reaction if disengaged or unable to act, and warm-starts once before the ordinary sweeps. Relaxation only recomputes frames and accumulates its deltas; commit writes the final reaction. Pause does not apply it; Resume retains it. Cadence changes are admitted only in Building and reinitialize the state; attempts while Paused refuse without changing the reaction. All 60/120/240 Hz commits use the same 480 Hz physical step, so no impulse rescaling rule is needed. Ratio/inertia authoring changes require recompile/reset. There is no phase-lock term or alternate transmission law.

## Chrome-observable acceptance (post-catalogue)

- Reverse, open and engaged modes in axis-aligned and rotated constructions; impulse transfer versus a disconnected control; source-loss control; signed wind and calm-air dissipation for the windmill.
- Solved pose/speed match the presentation; exact Reset and Save/Load of rotated constructions after every Run; same construction and inputs give the same outcome.
- Reviewer grep for `clutch`, `gearbox`, or `windmill` in the physics solver finds nothing element-keyed.
