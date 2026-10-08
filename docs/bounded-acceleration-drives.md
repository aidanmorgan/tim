# Bounded acceleration drives

A drive is declaration data on a hinge or slider constraint record of the single WASM SIMD f32 solver ([capability inventory](gpu-f16-physics.md#capability-inventory)); it is solved as a motor row in the same Box2D v3 TGS Soft solver pass as contacts and joints. Motor CAT-042, Conveyor CAT-019, Pusher CAT-039, Gate CAT-051, Shutter CAT-007 and every powered actuator declare drives; none has its own solver, and no catalogue identifier reaches the kernel.

## Declaration data

- Joint identity and axis kind: slider (target in m/s², effort in N) or hinge (target in rad/s², effort in N·m).
- Target generalised acceleration, or a target speed with its time horizon.
- A finite signed effort interval that contains zero.
- Source binding: the finite store or supply (battery, wound spring, gas chamber) debited for the work done, with braking/work accounting declared.
- Participants: at least one dynamic body; a static participant provides its declared motion and receives none.

Admission rejects unsupported, foreign or duplicate IDs, non-finite values, reversed effort bounds and bounds that exclude zero, atomically at compile.

## Behaviour for the player

- The solver chooses effort inside the interval together with joint reactions and Coulomb contact. Interior effort reaches the target within the [envelope](gpu-f16-physics.md#game-grade-envelope); at saturation the drive falls short in that direction.
- Reactions are equal and opposite on the two participants; an obstruction loads the source and stalls the drive instead of pushing cargo through.
- Work done is debited from the bound source; supply loss adds no effort and does not erase the momentum of an unpowered ideal hinge. Shaft stops, loads and damping are declared physical data, not cosmetic coast-down.
- Committed results per drive: effort, achieved acceleration and an enum-typed limit status, in canonical identity order; artwork, reported speed and travel events derive from committed physical motion.

## Chrome-observable acceptance (ELEMENT-n: CAT-005 Battery → CAT-042 Motor → CAT-019 Conveyor)

- A powered conveyor carries cargo; blocked cargo stalls and the belt artwork cannot move it; disconnected or exhausted supply produces no motion.
- Two competing drives on one shaft and a no-drive control behave differently and predictably; an overloaded drive saturates.
- Exact Reset and Save/Load after every Run; same construction and inputs give the same outcome.
- Audits of solver kernel dispatches confirm no element-keyed branching or catalogue identifiers.
