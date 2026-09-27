# Authoring forgiving puzzles

Difficulty assistance is automatic engine behavior, not a button, hint action, or hidden replacement of the whole machine. A level author controls it on each part instance.

## Where settings live

Each `PartSpec` in a puzzle's `parts` and `solution` arrays has a `difficulty` curve. Fixed parts carry their receiver/trigger rules. Solution parts additionally describe placement targets and permitted correction envelopes. The runtime copies these into `MachineData.placement_targets`; they are targets for assistance, not a requirement that the player reproduce the reference solution.

Each curve entry has a `precision` coordinate from 0 (forgiving) to 1 (strict). Intermediate settings interpolate the author's numeric values. Missing curves mean no placement correction or receiver guide. Curves may contain any number of knots.

A ramp's forgiving entry might include:

```json
{
  "precision": 0,
  "position_window": 0.5,
  "rotation_window": 10,
  "max_position_correction": 0.15,
  "max_rotation_correction": 3,
  "blend_seconds": 0.4
}
```

Distances are metres; rotations are degrees; blending time is seconds. This entry only corrects a ramp already within 0.5 m and 10 degrees of its authored target. It moves at most 0.15 m and turns at most 3 degrees. Authors can set either maximum to zero independently. The shipped strict knots set both to zero.

## Runtime behavior

At run start, movable static parts are matched to same-kind authored slots one-to-one by proximity and orientation. Exact placements reserve their slots. Fixed and dynamic parts are never repositioned by this system. An out-of-window placement is left alone; valid alternative solutions are still allowed and judged by goal events.

Eligible corrections are calculated once from the player's initial arrangement. They do not accumulate on every tick or across resets. Over the authored blend time (minimum 0.1 seconds), a quintic ease moves the part root and collider together. Quaternion interpolation uses the short rotational arc. There is no start-time snap or hidden offset between visible and collidable geometry. Reset reconstructs the original player placement.

This implementation gently repositions eligible static parts during the start of a run. It does not teleport balls or snap the player's build in edit mode. Rendered fluidity still needs browser inspection; native tests check continuity and bounded per-tick displacement.

The following fields independently control physical assistance:

- `guide_acceleration`: bounded lateral acceleration above a receiver's open mouth, only during descent.
- `capture_margin`, `capture_speed`, `capture_dwell`: receiver acceptance height, maximum speed, and required residence time.
- `trigger_threshold`: impact speed needed by switches or dominoes.

No global difficulty multiplier changes gravity or the simulation timestep. The separate surface-friction option is independent of these author-defined curves.

## Campaign tooling and checks

`tools/Campaign` is a C# authoring utility. It reads the first five tutorials and emits the complete composed campaign as JSON on stdout; it never overwrites files. Part-specific defaults in that utility are materialized into each JSON instance and can be customized by the author. Editing the generator and regenerating replaces manual changes to the generated campaign.

Tests cover smooth bounded correction, strict and out-of-window no-ops, reset without drift, unique slot assignment, angle wraparound, interpolation, and a spring-placement sweep whose receiver physics remain identical in both difficulty modes. All 40 authored solutions also run at forgiving, balanced, and precise settings.

These checks do not prove every alternative solution is preserved, every difficulty transition is monotonic, or the animation is visually satisfactory. Browser playthroughs and broader placement sweeps remain required.

