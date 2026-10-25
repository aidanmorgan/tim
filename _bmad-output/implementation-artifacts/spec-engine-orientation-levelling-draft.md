---
title: 'Engine: orientation levelling and two-point friction'
type: 'bugfix'
created: '2026-10-25'
status: 'draft'
predecessor: 'spec-engine-cylinder-creep.md (M1+M2, landed under owner decision "Land M1+M2, follow-up", 25 Oct 2026)'
baseline: 'story-9a-44 worktree at 9568500d + the uncommitted M1+M2 change (the single normal-branch rule: the band also in both per-row fallbacks)'
---

**Split into three slices (coordinator, 25 Oct 2026, from the owner directives).** This draft is the shared diagnosis, not one slice.
Section (c) is `engine-cap-block`, section (b) is `engine-two-point-friction` and section (a) is `engine-orientation-levelling`, delivered
in that order with `elements-visible-solid` and `engine-hollow-shapes` between the first and the second. Each slice restores the
removed tests it owns at their unchanged bounds. Stories and acceptance:
[epics.md engine and collider slices](../planning-artifacts/epics.md#engine-and-collider-slices).

# Outcome

A body resting on a line or face contact levels any tilt and stays put, and a lying cylinder landing on its side keeps its heading. Two
open defects remain after M1+M2. Both are recorded as an open defect, not an accepted bound, in the "Resting Drift" row of
`docs/gpu-f32-physics.md`:

- **(a) Sub-ulp orientation levelling (draft M3).** A residual tilt below about 5e-6 rad is never levelled when its per-substep levelling
  increment is below the ulp of the quaternion's large components (a lying body is turned about 90°, so those components are about
  0.5 or 0.707). The rotation stops changing, while the contact rows keep asking for the levelling rock every substep, so the centre
  walks along the axis at up to 2.2e-5 m/s.
- **(b) Landing yaw kick (draft D4).** A lying cylinder that lands on its side from a 0.17 m drop takes a one-off yaw of 1.77e-4 rad, from
  unequal friction impulses at the two generator ends. The kick depends on the order in which the friction rows are solved. The roll then
  runs straight at that heading, 1.25 mm off line over 7 m.

The four tests below were written for the predecessor slice and were removed from it red, unchanged in bounds. This slice restores them
and must turn them green. Their bounds, seeds and scenes stay as written: no loosening.

# Authority and constraints (carried over)

- AGENTS.md hard rules: no skipped tests, behavioural assertions only, one shared response for every pair, refactor forward, f32
  throughout, and no rest or velocity-snap threshold ("No rest threshold zeroes velocity").
- The energy gates `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`, its Lively twin, and
  `RandomDropsGainNoMoreEnergyThanABoxAndTheSlop` stay at their current bounds and seeds. Measured after M1+M2, 0/200 failed in every class:

  | Class | Dead (bound 2.045 mJ) | Rolling (bound 2.052 mJ) | Lively (bound 1.964 mJ) |
  | --- | --- | --- | --- |
  | CylinderOntoPlane | 1.747 | 1.595 | 1.445 |
  | CylinderOntoBox | 1.657 | 1.487 | 1.108 |
  | BoxOntoCylinder | 1.012 | 0.198 | 0.227 |
  | CylinderOntoLyingCylinder | 1.202 | 0.872 | 1.109 |
  | CylinderOntoStandingCylinder | 1.572 | 1.323 | 1.457 |
  | SkewedRollOffEdge | 0.002 | 0.002 | 0.000 |

  Values are worst per-tick rise in mJ (`scratchpad/creep-slice/gates-report.log`, measured under the single rule).
- `F32BehaviourIdentityTests` tolerances (12 ticks, 5.1 mm) stay unchanged.
- **Measuring drift:** the integrator moves a body by f32 world-coordinate steps, one ulp of the coordinate (1.49e-8 m at 0.2 m, about 1 µm
  at 8 m). Away from the origin a drift under about 3.6e-6 m/s reads as none and anything above it as 7.15e-6 m/s, one ulp per 480 Hz
  substep. All set-down cases therefore stand at the horizontal origin with the exact level quaternion (½, ½, ½, ½).

# The removed tests (exact code; restore into `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`)

The helpers they use remain in the file: `WeightRadius`, `WeightHalfWidth`, `WeightMaterial`, `AxisOf`, `FromHorizontal`, `AxialStep`,
`ExactlyLevel`, `SetDownAt` (`Vector3.Zero`), `Lowest`, `DriftSeed` (92000). `FromVertical` was removed with the box case; restore it too:

```csharp
    /// <summary>The angle in radians between a body-fixed direction and the vertical, from its horizontal part (accurate near zero).</summary>
    private static double FromVertical(Vector3 direction) =>
        Math.Asin(Math.Min(1, Math.Sqrt((double)direction.X * direction.X + (double)direction.Z * direction.Z) / Math.Sqrt(Squared(direction))));
```

## 1. Drop-ramp roll (world-z clause red: 1.249 mm at tick 1680 against 0.5 mm)

The committed test is today's flush-only `AWeightRollingOffAFlushRampRollsStraightAndStaysPutOnceStopped`. Restore the theory over both
ramps:

```csharp
    /// <summary>The 10.2a ramps, the default Ramp's 3 × 0.18 × 1.3 m slab at 15°. Flush: its top face's lower edge meets the bench, so a
    /// roll runs straight on. Drop: its bottom corner meets the bench, so a roll leaves the top face 0.17 m up and lands on its side.</summary>
    public enum WeightRamp { Flush, Drop }

    [Theory]
    [InlineData(WeightRamp.Flush)]
    [InlineData(WeightRamp.Drop)]
    public void AWeightRollingOffARampRollsStraightAndStaysPutOnceStopped(WeightRamp ramp)
    {
        // The 10.2a roll on LawScene: the Weight lying along world Z, 0.5 m downhill of the ramp's centre and 3 mm clear of its top face.
        // A straight roll keeps the centre within the 0.5 mm resting envelope of z = 0 and moves it at most that far along its own axis;
        // once stopped (below 1 mm/s, which leaves under 3 µm of Crr deceleration) it moves at most a tenth of the envelope, 0.05 mm.
        const double degrees = 15;
        var a = (float)(-degrees * Math.PI / 180);
        var normal = new Vector3(-MathF.Sin(a), MathF.Cos(a), 0);
        var downhill = new Vector3(MathF.Cos(a), MathF.Sin(a), 0);
        var centre = ramp switch
        {
            WeightRamp.Flush => At(0, (float)(BenchY + 1.5 * Math.Sin(degrees * Math.PI / 180) - .09 * Math.Cos(degrees * Math.PI / 180))),
            WeightRamp.Drop => At(0, BenchY + .475f),
            _ => throw new ArgumentOutOfRangeException(nameof(ramp), ramp, "Unknown ramp."),
        };
        var scene = new LawScene();
        // The Ramp declares the bench's material.
        scene.StaticBox(centre, At(1.5f, .09f, .65f), AboutZ(-degrees));
        var weight = scene.Cylinder(centre + .09f * normal + .5f * downhill + (WeightRadius + .003f) * normal, WeightRadius, WeightHalfWidth,
            4f, WeightMaterial, AlongZ);
        var world = scene.Admit();
        var tps = world.TicksPerSecond;
        var reads = new List<LawBody>();
        world.Step(14 * tps, (_, state) => reads.Add(state[weight]));

        var final = reads[^1];
        Assert.True(final.Position.X > centre.X + 2.5f, $"rolled off the ramp onto the bench (x {final.Position.X})");
        Assert.InRange(final.Position.Y, BenchY + WeightRadius - .0005f, BenchY + WeightRadius + .0005f);
        var stopped = reads.FindIndex(2 * tps, b => b.Speed < .001);
        Assert.True(stopped > 0 && stopped < reads.Count - 2 * tps, $"stopped at tick {stopped + 1}");

        var problems = new List<string>();
        var worstZ = reads.Select((b, tick) => (Z: MathF.Abs(b.Position.Z), Tick: tick + 1)).MaxBy(s => s.Z);
        if (worstZ.Z > .0005f) problems.Add($"world |z| {worstZ.Z * 1000:F3} mm at tick {worstZ.Tick} (bound 0.5 mm)");
        double axial = 0, worstAxial = 0;
        for (var i = 1; i < reads.Count; i++)
        {
            axial += AxialStep(reads[i - 1], reads[i]);
            worstAxial = Math.Max(worstAxial, Math.Abs(axial));
        }
        if (worstAxial > .0005) problems.Add($"moved {worstAxial * 1000:F3} mm along its own axis (bound 0.5 mm)");
        var after = Vector3.Distance(final.Position, reads[stopped].Position);
        if (after > .00005f) problems.Add($"moved {after * 1000:F4} mm after stopping at tick {stopped + 1} (bound 0.05 mm)");
        Assert.True(problems.Count == 0, $"{ramp} ramp: " + string.Join("; ", problems));
    }
```

## 2. Micro-tilt cases 1e-5, 1e-4 and the box on end (red: 2.53e-6, 7.58e-6, 7.52e-6 m/s against 1e-6)

The committed test is today's 2e-6-only `ALyingCylinderSetDownWithAMicroTiltLevelsAndDoesNotWalk`. Restore the theory:

```csharp
    public enum TiltedRest { LyingCylinder, BoxOnEnd }

    [Theory]
    [InlineData(TiltedRest.LyingCylinder, 2e-6)]
    [InlineData(TiltedRest.LyingCylinder, 1e-5)]
    [InlineData(TiltedRest.LyingCylinder, 1e-4)]
    [InlineData(TiltedRest.BoxOnEnd, 1e-4)]
    public void ABodySetDownWithAMicroTiltLevelsAndDoesNotWalk(TiltedRest shape, double tilt)
    {
        // The exact level pose tilted about a horizontal world axis (a lying cylinder about the transverse X, its end low; a box standing
        // on its end about Z), set down touching at its lowest point: tilt is the only difference from the level control. Levelling turns
        // it about its lowest support, which moves the centre by (arm) × tilt, at most 0.35 m × 1e-4 = 35 µm, within a few ticks; after
        // the first 2 s, the centre's drift over 8 s must stay under 1e-6 m/s (8 µm), and the tilt under 1e-5 rad.
        var scene = new LawScene();
        GpuBodyId body;
        Vector3 upright;
        if (shape == TiltedRest.LyingCylinder)
        {
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)tilt) * ExactlyLevel;
            body = scene.Cylinder(SetDownAt + At(0, BenchY + Lowest(q, WeightRadius, WeightHalfWidth)), WeightRadius, WeightHalfWidth, 4f,
                WeightMaterial, q);
            upright = Vector3.UnitY;
        }
        else
        {
            var half = At(.3f, .1f, .15f);
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)tilt) * ExactlyLevel;
            var lowest = half.X * MathF.Abs(Vector3.Transform(Vector3.UnitX, q).Y) + half.Y * MathF.Abs(Vector3.Transform(Vector3.UnitY, q).Y) +
                half.Z * MathF.Abs(Vector3.Transform(Vector3.UnitZ, q).Y);
            body = scene.Box(SetDownAt + At(0, BenchY + lowest), half, 2f, WeightMaterial, q);
            upright = Vector3.UnitX;
        }
        var world = scene.Admit();
        var tps = world.TicksPerSecond;
        // The admitted tilt (f32 rotation) is the requested one: a lying axis from the horizontal, a standing one from the vertical.
        double Tilt(LawBody b) => shape == TiltedRest.LyingCylinder ? FromHorizontal(AxisOf(b))
            : FromVertical(Vector3.Transform(upright, b.Rotation));
        var initial = Tilt(world.Initial(body));
        Assert.InRange(initial, .5 * tilt, 1.5 * tilt);
        LawBody? settled = null;
        var final = world.Step(10 * tps, (tick, state) => { if (tick == 2 * tps) settled = state[body]; })[body];
        var drift = final.Position - settled!.Value.Position;
        drift.Y = 0;
        var speed = drift.Length() / 8;
        Assert.True(speed < 1e-6 && Tilt(final) < 1e-5,
            $"{shape} tilted {initial:E2} rad: drift {speed:E2} m/s over 8 s (bound 1e-6), tilt {Tilt(final):E2} rad (bound 1e-5)");
    }
```

## 3. Lying-drop axial-drift clause (red: 180/200, worst 2.24e-5 m/s against 1e-6)

The committed `LyingDropsComeToRestLevelWithinTheRestingEnvelope` keeps the tilt, penetration and rest clauses. Restore the axial clause.
Its seeds are `DriftSeed + batch` for batch 0–3, 50 drops each, with the generator unchanged:

```csharp
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LyingDropsComeToRestLevelWithoutDriftingAlongTheirAxis(int batch)
    {
        // 50 random lying drops per batch (200 in all): r 0.1–0.4, half-width 0.1–0.6, 0.5–5 kg, axis tilted ±0.05 rad from level with a
        // random heading and turn about the axis, lowest point 0.5–150 mm up, Crr {0, 0.03, 0.1}, μ {0.1, 0.3, 0.6}, restitution 0–0.3.
        // At rest the centre's mean axial speed over the 6 s is under 1e-6 m/s (6 µm), the axis is level within 1e-5 rad, the resting
        // penetration is within the 0.5 mm envelope and the speed stays under 2 mm/s (a measure, never a threshold the engine applies).
        float[] rolling = [0f, .03f, .1f];
        float[] friction = [.1f, .3f, .6f];
        var rng = new Random(DriftSeed + batch);
        var failures = new List<string>();
        double worst = 0;
        for (var drop = 0; drop < 50; drop++)
        {
            float r = .1f + .3f * (float)rng.NextDouble(), h = .1f + .5f * (float)rng.NextDouble(), m = .5f + 4.5f * (float)rng.NextDouble();
            float tilt = (float)((rng.NextDouble() * 2 - 1) * .05), heading = (float)(rng.NextDouble() * 2 * Math.PI);
            float turn = (float)(rng.NextDouble() * 2 * Math.PI), height = .0005f + .1495f * (float)rng.NextDouble();
            var material = new LawMaterial(.3f * (float)rng.NextDouble(), .1f, friction[rng.Next(3)], rolling[rng.Next(3)]);
            var q = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitY, heading) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2 + tilt) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, turn));
            var scene = new LawScene();
            var body = scene.Cylinder(At(0, BenchY + Lowest(q, r, h) + height), r, h, m, material, q);
            var world = scene.Admit();
            var tps = world.TicksPerSecond;
            double axial = 0, fastest = 0;
            LawBody? previous = null;
            var final = world.Step(10 * tps, (tick, state) =>
            {
                var b = state[body];
                if (tick > 4 * tps)
                {
                    axial += AxialStep(previous!.Value, b);
                    fastest = Math.Max(fastest, b.Speed);
                }
                previous = b;
            })[body];
            var creep = Math.Abs(axial) / 6;
            worst = Math.Max(worst, creep);
            var level = FromHorizontal(AxisOf(final));
            var penetration = BenchY - (final.Position.Y - Lowest(final.Rotation, r, h));
            if (creep >= 1e-6 || level >= 1e-5 || penetration > .0005 || fastest >= .002)
                failures.Add(FormattableString.Invariant(
                    $"#{drop} r {r:F3} h {h:F3} m {m:F2} tilt {tilt:F4} drop {height * 1000:F1} mm e {material.Restitution:F2} μ {material.Friction} Crr {material.Rolling}: axial {creep:E2} m/s, level {level:E2} rad, penetration {penetration * 1000:F3} mm, speed {fastest:E2} m/s"));
        }
        Assert.True(failures.Count == 0, FormattableString.Invariant($"{failures.Count}/50 failed (worst axial {worst:E2} m/s):") +
            Environment.NewLine + string.Join(Environment.NewLine, failures));
    }
```

# Measured residuals (25 Oct 2026, M1+M2 under the single rule; identical with the flag off in the fallbacks)

## (a) Sub-ulp orientation levelling (M3)

| Case | Drift (bound 1e-6 m/s) | Final tilt | Rotation bit-frozen from | Baseline (no M1, no M2) |
| --- | --- | --- | --- | --- |
| Cylinder set down tilted 2e-6 rad | passes (committed) | levelled | — | 1.40e-4 m/s (red) |
| Cylinder set down tilted 1e-5 rad | 2.53e-6 m/s | 5.96e-7 rad | tick 3 | passes (M1 exposes M3 here) |
| Cylinder set down tilted 1e-4 rad | 7.58e-6 m/s | 1.79e-6 rad | tick 46 at (0.3, −0.2); frozen at the origin | 1.40e-4 m/s |
| Box on end tilted 1e-4 rad | 7.52e-6 m/s | 2.29e-6 rad | tick 23 | 6.09e-6 m/s (red at baseline) |

- **Lying-drop sweep (200 drops):** 180 fail the axial clause, at 1.0e-6 to 2.24e-5 m/s. Worst per batch: 1.25e-5, 2.24e-5, 1.21e-5,
  1.32e-5. Every other clause passes: level within 4.7e-6 rad, penetration 0.268–0.272 mm, speed under 0.36 mm/s.
- **Attribution over all 180 failing drops** (committed reads only; `scratchpad/creep-slice/m3-split.log`):
  - 126 have their quaternion bit-identical across the whole 4–10 s window, while the centre advances along the axis. This is the M3
    signature: the committed angular velocity is a constant sub-ulp pitch, for example 6.6e-9 rad/s (1.4e-11 rad per substep, against a
    quaternion ulp of about 6e-8).
  - 54 have a changing quaternion. In 48 of them it is because Crr is 0, so a slow residual roll (spin 1e-8 to 1.3e-3 rad/s) keeps
    turning the body. The other 6 have Crr 0.03 or 0.1 and spin of 2e-8 to 5e-7 rad/s. In all 54 the tilt does not level: axis y at 4 s
    equals axis y at 10 s within a few 1e-7.
- **Traces:** `m3-attribution.log` (micro-tilt 1e-4 and 1e-5, sweep cases 0#0, 1#1 and 2#0); `followup-residuals-noFB.log` (all four
  removed tests, plus the box-on-end and 1e-5 traces at the origin).
- **The level control is not the defect.** At the origin a level cylinder shows 1.3e-11 m/s of solver round-off, also at baseline. The
  committed control bounds it at 1e-9 m/s.

## (b) Landing yaw kick (D4)

Drop ramp, `scratchpad/creep-slice/d4-trace.log`, `d4-yaw.log`, `followup-residuals-noFB.log`:

- **On the ramp:** yaw (the axis's x component) stays within ±6e-6.
- **At the bench landing (ticks 156–161):** yaw jumps to −1.77e-4 rad. It then stays constant through the roll (−1.75e-4 to −1.71e-4),
  so world z grows linearly: 0.039 mm at tick 180, 0.65 mm at tick 480, 1.22 mm when stopped at about tick 1200.
- **After stopping:** z grows a further 0.028 mm in 4 s. That is M3 axial walking at about 6.9e-6 m/s, within the 0.05 mm clause.
- **The flush ramp has no such kick:** its yaw stays within ±1.3e-5 and z within 0.07 mm. That test is committed and passes.

Per-row yaw impulse attribution: probe-only instrumentation of the world-Y angular velocity change on the Weight, summed per tick:

- **Normal rows contribute no yaw by construction:** the bench normal is vertical (rN about 1e-10).
- **Tick 160 (the main impact)**, net wy −2.159e-2 rad/s:
  - solve friction, point 0: +9.04e-2; points 1+: −6.87e-2;
  - relax friction, point 0: −3.01e-2; points 1+: +3.26e-2;
  - restitution-pass friction: −2.41e-2;
  - warm start: −2.16e-2;
  - rolling rows: about 1e-8.
- **Tick 161:** the line contact's friction removes the yaw rate (wy −4.1e-5), leaving 1.774e-4 rad of heading. That is −2.16e-2 rad/s
  over about one tick, which matches.
- **The fallback is in play:** during the landing ticks the normal block falls back to the sequential per-row solve in 28–32 of the
  32 sweeps per tick. The block solution would make an accumulated impulse go negative.

Friction-order evidence, as final world z (drop / flush ramp):

| Probe mode | Drop ramp | Flush ramp |
| --- | --- | --- |
| Production order (points 0 → n) | +1.249 mm (yaw −1.61e-4) | +0.067 mm |
| Friction rows reversed (n → 0) | −0.280 mm (yaw −7.2e-7) | −0.036 mm |
| Friction order alternated every sweep (symmetric Gauss–Seidel) | +1.265 mm (yaw −7.6e-4) | +1.159 mm |

- Reversing the order "fixes" this scene only by the luck of the order.
- Alternating the order made both ramps worse: the yaw then drifts steadily during the roll. That was attempt 1 of 3 in the predecessor
  slice.

## (c) Pre-existing: an ill-posed normal block for coplanar face and cap manifolds

This was found while diagnosing the rod-righting change in the predecessor slice (`scratchpad/creep-slice/rod-diagnosis.log`, probe
`CreepSliceProbe.RodDiagnosis`). The rod is r 0.1, h 0.4, 1 kg, Dead material, released at 10.5° or 12.5° on its rim, run for 6 s.

- **Why the block is ill-posed:** a cap resting on a plane yields four coplanar points. Their normal Jacobians span only three rigid
  motions: the normal translation and two tilts. So `SolveNormalBlock`'s 4×4 coupling matrix (`CouplingMass`) has rank 3, and only the
  1e-3·trace/n diagonal regularisation (`BlockRegularization`) makes it invertible.
- **What the solve returns:** a huge alternating solution in the null space. One recorded sweep (12.5°, azimuth 45, under the single rule)
  had separations −255.4, −55.3, +144.7 and −55.3 µm and returned solutions of −33.8, +33.7, −33.7 and +33.7 N·s on a 1 kg body.
- **What follows:** an accumulated λ goes negative, and the block falls back to the sequential per-row solve.
- **How often** (6 s run, single rule; the matrix is never singular to the pivot test, so every fallback is negative-λ):

  | Release | Block solves | Negative-λ fallbacks | Of which with the band active |
  | --- | --- | --- | --- |
  | 12.5°, az 0 | 14,816 | 7,700 (52 %) | 544 |
  | 12.5°, az 22.5 | 13,408 | 8,357 (62 %) | 421 |
  | 12.5°, az 45 | 12,848 | 9,328 (73 %) | 604 |
  | 10.5°, az 45 | 15,984 | 8,581 (54 %) | 645 |

- **Energy kept per rim impact.** Under the single rule it is 0.833–0.856 on the early impacts. A perfectly inelastic rigid impact keeps
  0.8307; the one-substep model allows up to 0.847–0.850 there; 3 of 41 measured impacts exceed that, by up to 0.008. With the per-point
  fallback, which matches baseline, it is about 0.76. The settle under 1° is 4.09 s (az 22.5) and 4.48 s (az 45) under the single rule,
  against 3.0–3.2 s per-point.
- **The predecessor's rod oracle:** it was re-derived from this model, with a declared +0.01 retention margin (T = 5.09 s from 10.5° and
  5.51 s from 12.5°). A well-posed block solve should bring the retention under the model without the margin. If it does, remove the
  margin from `InelasticRockingSettleSeconds`.

**Proposal:** a well-posed block solve for coplanar face and cap manifolds. Candidates:

1. **Reduce to rank.** When the points are coplanar (for example, the 4×4 matrix's smallest pivot is below a relative tolerance), solve
   for the three wrench components instead: normal force and two moments about the manifold centre. Then distribute them to the points
   by the minimum-norm non-negative split. If a point would pull, drop it and re-solve with the rest (a bounded active set of at most 4
   iterations).
2. **Box2D v2 two-point enumeration, extended to four.** Enumerate the active sets in a fixed order (all four, the three-point subsets,
   then two, then one) and accept the first whose impulses are non-negative and whose inactive points do not approach. The count is fixed
   and there is no convergence loop.
3. **Box2D v3 practice.** Drop the block solve for manifolds of more than two points, and use sequential rows with anchor or relax
   ordering. This removes the fallback asymmetry, but the order dependence then needs the two-point friction work below.

Acceptance:
- the rod's per-impact retention is at or below the one-substep model;
- 0 fallbacks on resting caps and faces;
- the energy gates and `F32BehaviourIdentityTests` unchanged;
- the box face sweep still drifts at most 1e-9 m/s.

# Candidate designs

## A. Orientation precision that keeps the energy gates

1. **Compensated f32 orientation remainder (draft C, rejected as it stood).**
   - What it is: a 4-lane f32 quaternion remainder kept by TwoSum per component, which integrates sub-ulp increments.
   - It ends the creep in every case: sweep worst 1.3e-7 m/s, micro-tilt 5e-11, box on end 1e-16.
   - **It breaks `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`**: CylinderOntoBox Dead, worst 4.109 mJ against a bound of
     2.043 mJ. Before reusing it, find out why: it may be the remainder releasing stored rotation into a contact (an energy injection),
     or the scratch form keeping its state on the pooled `SimBody`, which is hidden state.
   - It also needs committed state: either 16 bytes, or 12 bytes for the compact form ρ = 2·vec(lo ⊗ q̄). Body record bytes 148–159
     are unreferenced by any literal offset; prove that with layout and codec tests, or extend the record. A changed record is refused
     at every boundary, never migrated. That needs an owner decision on the committed pose.
2. **Integrate the levelling about the contact pivot, not the centre of mass.** Rotating about the lowest support moves no material
   point, so the sub-ulp rotation and the r·W translation drop out together. The translation would be rounded the same way as the
   rotation, rather than integrated while the rotation is dropped. This is untested; check it against the gates.
3. **A canonicalised principal frame per body.** Store the orientation relative to the nearest of the 24 axis-aligned rotations, so a
   lying body's quaternion has small components with fine ulps (the box-yaw control in the predecessor diagnosis levelled to 1e-8 rad
   exactly because its increment landed in small components).
   - It changes the body record and the committed pose, so it needs an owner decision.
   - Presentation reads the composed q.

Whatever the design, every PRIMARY test above and the energy gates must pass at their unchanged bounds and seeds. Also re-verify the
Reset bit-identity test and save/load continuation of a resting tilted body.

## B. A simultaneous two-point friction solve

1. **Solve a manifold's tangent rows as one block.**
   - What it is: a manifold's tangent rows (2 per point) as one regularised block, with the Coulomb cones projected per point afterwards,
     or a two-point LCP enumeration (Box2D v2.x-style block solver) for the normal rows paired with a symmetric friction solve. This
     removes the order dependence that gave the 1.77e-4 rad kick.
   - Risk: it touches every multi-point contact, so the energy gates, `F32BehaviourIdentityTests` (12 ticks, 5.1 mm),
     `LaunchLessonSolveTests` and the restitution laws must all re-pass.
2. **Central friction anchor plus twist row.**
   - What it is: friction for a manifold applied at its centre, as one 2-D tangent row plus one twist (normal-axis) row bounded by μ·ΣλN
     times the manifold's radius, in place of per-point friction rows (Box2D v3 "anchor" friction).
   - Yaw then comes only from the twist row, which is symmetric.
3. **Two-point normal LCP enumeration only, keeping per-point friction.**
   - What it is: replace the sequential per-row fallback with Box2D v2's enumeration (both active, one active, none). Then unequal normal
     loads come from geometry, not from solve order.
   - The predecessor showed the fallback runs in nearly every sweep while landing.
   - Check first that the yaw follows the normal-impulse asymmetry: log λN at both ends per sweep during ticks 156–161.

# Evidence

All of it is in `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/creep-slice/`:

- `baseline-primary.log`: the PRIMARY tests red before M1+M2.
- `fixed-primary.log`, `final-cyl.log`: after M1+M2, with the fallback flag on.
- `followup-residuals-noFB.log`: the four removed tests, the sweep split, the box-on-end and 1e-5 traces, and both ramp traces with
  per-row yaw, measured with the fallback flag off. The same tests under the single rule give identical numbers (`fixed-primary.log`,
  `m3-split.log`, `d4-yaw.log`).
- `rod-diagnosis.log`: the rod-righting finding under the single rule. It belongs to the predecessor slice, not to this one: fallback
  causes, band activity, and the energy kept per impact against the rigid-body limit.
- `m3-attribution.log`, `m3-split.log`: the M3 signature (frozen rotation while the centre walks) and the 180-case split.
- `d4-trace.log`, `d4-yaw.log`: drop and flush ramp yaw and z over time, and the per-row yaw impulses through the landing.
- `rod-summary.log`: the rod-righting settle times with the fallback flag on and off (on delays settling; off matches baseline).
- `mutation-noM1.log`, `mutation-noM2.log`, `origin-noM1.log`, `origin-noM2.log`, `origin-noM1+noM2.log`,
  `committed-mutation-noFB+noM1.log`, `committed-mutation-noFB+noM2.log`: the mutation runs.
- `gates-report.log`: the energy-gate table.
- `rod-oracle-mutation-*.log`: the re-derived rod oracle under each probe mutation.
  - `rim85`, cap rim points at 0.85·r: red at every azimuth (the 12.5° rod topples).
  - `maxE`, `halfE`, `bias10`, `spec10`, `zeta05`: green. The settle clause has no mutation shown to fail it yet.
- `probe/`: the scratch copy with the `CREEP_PROBE` toggles (noM1, noM2, noFB, revF, revFB, altF), the per-row yaw instrumentation,
  and `CuriousContraptions.tests/CreepSliceProbe.cs` (Level, MicroTilt, Sweep, SweepSplit, BoxOnEndTrace, Ramp, RodSummary, Rod). These
  are diagnosis only; never copy the toggles into production.
- `../spec-engine-cylinder-creep-draft.md`: the Half-era diagnosis of M1–M3, the measured effect of C, and the D4 notes.
