# Removed observational instrumentation

These exact replacement pairs produced the two-case diagnostic. They were removed before the final candidate build. They record no accepted runtime change. Stage values in signed-trace.csv: 0 Begin, 1 Gravity, 2 IntegrateBefore, 3 IntegrateAfter, 4 End, 5 NormalBefore, 6 NormalAfter, 7 FrictionBefore, 8 FrictionAfter, 9 TorqueBefore, 10 TorqueAfter. Count each impulse once from its After row; Before rows establish application state. The scene has zero local centre offset, so recorded position is its centre of mass.

## CuriousContraptions.Simulation/WorkshopCollisionKernels.cs

Before:

```csharp
internal static partial class WorkshopCollisionKernels
{
```

After:

```csharp
internal static partial class WorkshopCollisionKernels
{
    internal enum DiagnosticStage { Begin, Gravity, IntegrateBefore, IntegrateAfter, End, NormalBefore, NormalAfter, FrictionBefore, FrictionAfter, TorqueBefore, TorqueAfter }
    internal static Action<DiagnosticStage, SimBody, Vector3, Vector3>? Diagnostic;
    internal static void Observe(DiagnosticStage stage, SimBody body, Vector3 impulse = default, Vector3 arm = default) =>
        Diagnostic?.Invoke(stage, body, impulse, arm);
```

Before:

```csharp
public static void ApplyImpulse(SimBody b, Vector3 p, Vector3 r)
```

After:

```csharp
public static void ApplyImpulse(SimBody b, Vector3 p, Vector3 r, DiagnosticStage stage = DiagnosticStage.NormalBefore)
```

Before:

```csharp
        b.Vx += p.X * b.InvMass;
```

After:

```csharp
        Observe(stage, b, p, r);
        b.Vx += p.X * b.InvMass;
```

Before:

```csharp
        b.Wz += dw.Z;
    }

    public static Vector3 PointVelocity
```

After:

```csharp
        b.Wz += dw.Z;
        Observe(stage == DiagnosticStage.NormalBefore ? DiagnosticStage.NormalAfter : DiagnosticStage.FrictionAfter, b, p, r);
    }

    public static Vector3 PointVelocity
```

## CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs

Before:

```csharp
                b.Vx += b.Gx * dt;
```

After:

```csharp
                WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.Begin, b);
                b.Vx += b.Gx * dt;
```

Before:

```csharp
                b.PreSolveKinetic = WorkshopConstraintSolver.Kinetic
```

After:

```csharp
                WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.Gravity, b, new Vector3(b.Gx, b.Gy, b.Gz) * (b.Mass * dt));
                b.PreSolveKinetic = WorkshopConstraintSolver.Kinetic
```

Before:

```csharp
                var prevCellX = b.CellX;
```

After:

```csharp
                WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.IntegrateBefore, b);
                var prevCellX = b.CellX;
```

Before:

```csharp
                WorkshopCollisionKernels.UpdateDynamicFrame(b);
            }

            foreach (var joint
```

After:

```csharp
                WorkshopCollisionKernels.UpdateDynamicFrame(b);
                WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.IntegrateAfter, b);
            }

            foreach (var joint
```

Before:

```csharp
            _contactCache.Clear();
            _frictionCache.Clear();
```

After:

```csharp
            foreach (var b in _dynamicBodies) WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.End, b);
            _contactCache.Clear();
            _frictionCache.Clear();
```

## CuriousContraptions.Simulation/WorkshopFrictionSolver.cs

Before:

```csharp
ApplyImpulse(c.A, force, p.AnchorA);
```

After:

```csharp
ApplyImpulse(c.A, force, p.AnchorA, WorkshopCollisionKernels.DiagnosticStage.FrictionBefore);
```

Before:

```csharp
ApplyImpulse(c.B, -force, p.AnchorB);
```

After:

```csharp
ApplyImpulse(c.B, -force, p.AnchorB, WorkshopCollisionKernels.DiagnosticStage.FrictionBefore);
```

Before:

```csharp
        var dw = WorkshopCollisionKernels.ApplyInvInertia(body, torque);
```

After:

```csharp
        WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.TorqueBefore, body, torque);
        var dw = WorkshopCollisionKernels.ApplyInvInertia(body, torque);
```

Before:

```csharp
        body.Wx += dw.X; body.Wy += dw.Y; body.Wz += dw.Z;
```

After:

```csharp
        body.Wx += dw.X; body.Wy += dw.Y; body.Wz += dw.Z;
        WorkshopCollisionKernels.Observe(WorkshopCollisionKernels.DiagnosticStage.TorqueAfter, body, torque);
```

## CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs

Before:

```csharp
    // ---- inertia ----
```

After:

```csharp
    [Theory]
    [InlineData(22.5)]
    [InlineData(45.0)]
    public void TemporarySignedRodDiagnostic(double azimuth)
    {
        var rows = new System.Text.StringBuilder();
        var substep = 0;
        WorkshopCollisionKernels.Diagnostic = (stage, b, p, arm) =>
        {
            if (stage == WorkshopCollisionKernels.DiagnosticStage.Begin) substep++;
            if (substep < 500 || substep > 850) return;
            rows.AppendLine(FormattableString.Invariant($"{substep},{(int)stage},{b.Px:R},{b.Py:R},{b.Pz:R},{b.Qx:R},{b.Qy:R},{b.Qz:R},{b.Qw:R},{b.Vx:R},{b.Vy:R},{b.Vz:R},{b.Wx:R},{b.Wy:R},{b.Wz:R},{p.X:R},{p.Y:R},{p.Z:R},{arm.X:R},{arm.Y:R},{arm.Z:R}"));
        };
        try
        {
            var scene = new LawScene();
            var q = Tipped(12.5, azimuth);
            scene.Cylinder(At(0, BenchY + Lowest(q, .1f, .4f) + .0005f), .1f, .4f, 1f, Dead, q);
            scene.Admit(SimulationCadence.Hz240).Step(430);
        }
        finally
        {
            WorkshopCollisionKernels.Diagnostic = null;
            TestContext.Current.TestOutputHelper!.WriteLine(rows.ToString());
        }
    }

    // ---- inertia ----
```
