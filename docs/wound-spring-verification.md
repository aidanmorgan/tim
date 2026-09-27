# Latched wound-spring launcher — energy groundwork

28 September 2026. **Incomplete component.** The finite spring primitive is implemented;
there is no playable wound-spring launcher, catalogue entry or browser proof yet.
Do not confuse this with the existing always-ready springboard or passive trampoline.

## Model and limits

`LatchedSpringStore` represents a linear spring compressed by an ideal ratcheted
screw. Stored energy is one half stiffness times compression squared; force is
stiffness times compression. These relationships follow
[OpenStax's spring-work derivation](https://openstax.org/books/university-physics-volume-1/pages/7-1-work).
The screw lead is compression per shaft radian, so ideal required input torque is
spring force times lead. The screw/ratchet, latch rules and future part geometry
are our design, not verified historical TIM behaviour.

The caller must supply actual positive shaft travel, available torque and
collision-cleared compression travel. Winding is capped by stroke, clearance
and available torque. Only the accepted travel adds work; excess offered travel
does not charge the spring. Reverse rotation freewheels. Zero drive preserves
charge. A reduced torque limit never unwinds an already latched spring.

A trigger releases the latch but contributes no energy and does not jump the
plunger or a payload. Empty triggers are not queued. Once released, winding is
disconnected until the physical stroke ends and the caller rearms the latch.
Only actual permitted extension debits spring potential energy. Returned work
must become plunger/load motion or explicitly accounted dissipation in the
future physical part. A blocked extension consumes nothing. Repeated triggers
cannot release twice, nor can a partially compressed release be relatched.
Reset clears compression, accounting and latch state.

States/results are enums. Invalid/non-finite inputs are rejected before state
changes. Double internal calculations keep products of finite float inputs
representable. Sub-precision movement adds/releases no work.

This is **not** a collision solver, dynamic screw/gear model, motor inertia
simulation or shared torque/power allocator. The existing mechanical network
propagates ideal signed shaft speed and has no source torque budget. Before
the part can claim belt-work conservation, its integration must explicitly
define where available torque comes from and how accepted work is supplied.
Do not infer energy from an arbitrary nonzero speed or silently add a torque
fallback. Broader source sharing/load feedback remains a separate network task.

## Native verification

26 focused cases cover:

- Rising work/force with compression, stroke limits and collision-clearance limits.
- Zero/insufficient torque, capped winding and accepted work versus input torque/travel.
- Stopped and reverse drive retaining latched charge.
- Partial/full release, blocked release, winding interlock and explicit rearming.
- Empty/no-queued triggers, repeated releases and 1,000 partial charge/release cycles.
- Step partitioning, sub-precision movement, Reset after an interrupted release.
- Invalid configuration/input atomicity and 27 combinations of extreme finite float scales.

`dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: **1,040 passed**, including all 26 focused spring cases (40-second test run).

`dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`: **passed**.

These native checks are prerequisites, **not** the mandatory Playwright evidence
for a completed puzzle element. No gameplay caller has changed in this groundwork.

## Remaining implementation and acceptance

- Define an explicit mechanically supplied winding drive; keep source torque/work limits honest.
- Add a finite-mass plunger with collision-swept movement and real payload contact.
  Account released work into plunger/load kinetic energy, gravity and dissipation;
  blocked geometry must never grant a free impulse. Payload mass must matter.
- Render a continuous helix, moving plate, gold latch and readable charge marks
  using the existing cream/navy/cyan/gold palette. Physical and visual poses agree.
  Add an original icon and scene/catalogue entry without a new toolbar.
- Define fixed-tick trigger ordering, empty stroke and obstruction behaviour.
  Support rotated placement and exact Run/Reset/current-schema restoration.
- Real-UI Playwright constructions must cover winding/release, no drive, reverse
  drive, retained charge after drive loss, partial charge, repeated trigger without
  recharge, blocked winding/release, empty shot and different physical payloads.
  Verify actual typed links and configurations; retain failed attempts.
- Prove integration with motor/belt and a receiving puzzle part, review continuous
  spring/latch motion, then commit and push the individually verified element.
- Add progressive lessons within the 75-level campaign; keep mobile and broader
  interacting-load coverage explicit.
