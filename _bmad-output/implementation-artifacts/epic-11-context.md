# Epic 11 Context: Rotary Drive & Mechanical Actuators

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the rotary drive family as declarative actuators: the Electric Motor, Conveyor Belt, Mechanical Clutch, Reverse Transmission, Linear Pusher, Wound Spring Motor and Toy Cannon. Players can then build powered drivetrains in which electrical supply turns a finite-inertia shaft. That shaft carries cargo on belts, is engaged, isolated or reversed by couplings, winds and releases stored spring work, and launches balls. Every one of these flows respects a single mechanical work ledger, so no load ever gains energy it was not paid for. These shafts and drives are the foundation for the later windmill, bellows and solar-panel elements.

## Stories

- Story 11.1: Electric Motor & Conveyor Belt (aggregate parent; closes with 11.1a–11.1b)
- Story 11.1a: Motor
- Story 11.1b: Conveyor
- Story 11.2: Mechanical Clutch & Reverse Transmission (aggregate parent; closes with 11.2a–11.2b)
- Story 11.2a: Clutch
- Story 11.2b: Reverse transmission
- Story 11.3: Linear Pusher Telescopic Actuator (not split; individual story)
- Story 11.4: Wound Spring Motor & Toy Cannon (aggregate parent; closes with 11.4a–11.4b)
- Story 11.4a: Wound spring
- Story 11.4b: Cannon

## Requirements & Constraints

- **Motor:** a supplied shaft delivers bounded torque and work. An unpowered shaft delivers none. A stalled or locked shaft never creates work. Supplied work is cumulative and committed, and it resets to 0 on Reset. An activation wire never creates supply.
- **Battery→Motor integration:** this check moved here from Story 8.1. A Battery cabled to the Motor spins its axle, and losing supply stops the drive immediately. Story 8.1 cannot fully close until this check lands.
- **Conveyor:** a driven belt transports contacting cargo through real friction, with equal and opposite reaction on a finite-inertia shaft or carrier.
  - A stopped drive stops transport and a reversed drive reverses it.
  - An unloaded belt creates no transferable work.
  - Several loads share one shaft. Side and underside contacts stay ordinary contacts.
  - The belt never copies a speed as a work source.
- **Clutch:** engagement transfers paid torque and disengagement isolates the load. Slip and capacity boundaries obey the work ledger.
- **Reverse transmission:** the output reverses relative to the input at the declared ratio (1:1). No input means no work, and reversal never increases the transmitted energy.
- **Wound spring:** a declared preload releases finite shaft work. An empty store releases nothing, and winding cannot exceed capacity.
- **Cannon:** an admitted, seated load launches from paid stored work. An unpaid, empty, unloaded or blocked cannon does not fire. Recoil and store bounds hold.
- **Linear Pusher:** on command the head extends at its rated speed and pushes a resting Bowling Ball. At maximum stroke it halts and holds.
- **One mechanical energy network:** source, route and load take part in one motion and work calculation. Downstream parts never get independent motor commands or copied speed and torque.
- **Validation:** wiring from a non-Mechanical domain into a mechanical socket is rejected. Invalid, non-finite or out-of-range parameters are refused atomically.
- **Lifecycle:** Reset restores exact shaft angle, speed, cosmetic state and work totals. Save/Load round-trips parameters, poses and typed connections.
- **Verification:** native C# tests run the real production simulation and cover positive, control, boundary, tick-progression and Reset/Save/Load cases. Each named Chrome suite (`tools/e2e/cat-*.test.ts`) is deferred to the post-catalogue E2E phase, so browser qualification stays pending.
- **Open owner decisions. Surface them; do not resolve them silently:**
  - When power is cut, does the motor or belt "decelerate to a halt under friction" (story text), or keep its momentum and coast (requirement and legacy)? If it stops, which explicit damping law stops it?
  - The "12 V" supply: no voltage quantity is declared anywhere.
  - Does motor or clutch-coil power debit the finite battery store, or is supply binary?
  - Is the legacy per-tick work allowance (torque·speed·Δt) and power cap (torque·speed) kept as the bounded-supply law?
  - Clutch disengagement: the story says "via signal" and "freewheels to a stop". The requirement says coil supply loss, with each shaft keeping its own momentum.
  - Pusher: the story uses a 1.0 m stroke against a 2.4 m catalogue default, and it is wired to a Switch, which is activation-only while the pusher's inputs are electrical.
  - Cannon muzzle speed: the story says 12 m/s, but the legacy 45 J store gives about 9.5 m/s.
  - Wound spring: confirm the pure-potential, undamped law.

## Technical Decisions

- Each element lives in its own `elements/<Name>/` package. The package holds the scene, colliders, `Loc_Port_<Id>` locators, mass and material data, typed parameters (closed enums, for example `MotorParameter { Speed, Torque }`) and bindings. No element has its own solver, kernel branch, class or tick loop. The motor row, ratio coupling, driven-surface friction and stores are generic capabilities.
- This epic introduces the **`Mechanical` connection domain** with `Drive` (output) and `DriveIn` (input) sockets.
  - Shafts couple at +1 ratio. The reverser adds a −1 ratio row.
  - Mechanical sockets are clockwise-positive viewed from +Z, so the motor `Drive` binds at −1 coordinate per radian about the hinge's +Z axis.
  - Connections validate domain equality and Output→Input direction.
- Shafts are dynamic cylinder bodies on revolute hinges, using the existing cylinder collider and inertia and the hinge capability.
- A **bounded motor row** (target speed, maximum torque, work and power budget) drives the shaft hinge. Zero effort disables actuation, so an unsupplied rotor coasts.
- Clutch engagement is an enum (Engaged/Open). The spring needs a one-way ratchet, latch/trigger/rearm state and typed results (`Released`/`Empty`/`AlreadyReleased`). The cannon needs a supply-charged store, a chamber containment sensor, a coalesced trigger, a muzzle swept-clearance query and an axial release impulse.
- Electrical supply uses the compiled supply routes. Unpowered consumers go inert without faulting.
- Cosmetic state (the supply lamp, rotor index, tread, arrows and belt links) is driven by committed shaft state on the animation worker and is never a physics target. The motor's supply lamp needs a new feedback source.
- These rules apply to every story:
  - f32 game values and the game-grade envelope.
  - Typed commands with receipts, committed-state queries and one-shot events on the event ring.
  - Forward-only changes: each slice deletes the legacy it supersedes.
  - Strict TypeScript for any non-C# glue.

## UX & Interaction Patterns

Use the standard Workshop loop: place a part, connect sockets through the contextual Connect (supply cables end at the actual sockets), adjust typed sliders, Run, observe, then Reset. The rotor and belt art follow the committed pose. The lamp blends from slate to gold while supplied. Keep the approved palette and follow the mechanical-work feedback rules in DESIGN.md.

## Cross-Story Dependencies

- Children are strictly serial: none starts until the previous child has passed acceptance and been published. Each aggregate parent closes only when all its children are done.
- **11.1a** needs the Battery (8.1), the cylinder (10.1) and the hinge (10.3). It gates 11.1b, 11.2a and 11.4a.
- **11.2b** needs 11.1a and 11.2a. **11.4b** needs 11.4a.
- **11.3** reuses the bounded slider drive from the Powered Gate (8.2) and the slider from 6.4. It needs an electrical command source. Its sensor→delay→pusher integration needs the Conveyor and Pipe.
- **11.4a/b** also use the preload store (6.5) and the hollow sleeve and collar colliders (6.6–6.7). The cannon feeder integration uses 8.2, 9.3 and 9.4.
- **Downstream:** the Windmill (12.3) needs 11.1–11.2, the Bellows needs the 11.1 Conveyor, and the Solar Panel (Epic 13) needs a Motor load.
