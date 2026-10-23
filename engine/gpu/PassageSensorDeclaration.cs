using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuPassageSensorId(ulong Value);

public readonly record struct PassageSensorDeclaration(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    Metres Radius,
    Metres RearmClearance)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0) throw new ArgumentException("Invalid passage sensor identity.");
        Rotation.Validate();
        PhysicsDeclarationBounds.Range(Radius.Value, (Half)0.01, (Half)10);
        PhysicsDeclarationBounds.Range(RearmClearance.Value, (Half)0.001, (Half)10);
    }
}

public enum PassageSensorPhase : uint { Unarmed, Armed, Fired }

public readonly record struct PassageSensorRead(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    GpuBodyId Body,
    GpuColliderId Collider,
    PassageSensorPhase Phase,
    uint EventOrdinal,
    Half EventPhase,
    LinearSpeed Speed);

public readonly record struct PassageBodyState(
    GpuPassageSensorId Sensor,
    GpuBodyId Body,
    GpuColliderId Collider,
    float Radius,
    PassageSensorPhase Phase,
    float PreviousLocalX,
    float PreviousLocalY,
    float PreviousLocalZ,
    ulong PassedCount);

public sealed class PassageSensorTracker
{
    public static readonly PassageSensorTracker Empty = new([], []);

    private readonly PassageSensorDeclaration[] _sensors;
    private readonly PassageBodyState[] _states;

    public IReadOnlyList<PassageSensorDeclaration> Sensors => _sensors;
    public IReadOnlyList<PassageBodyState> States => _states;

    public PassageSensorTracker(PassageSensorDeclaration[] sensors, PassageBodyState[] states)
    {
        _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
        _states = states ?? throw new ArgumentNullException(nameof(states));
    }

    public static PassageSensorTracker Create(
        ReadOnlySpan<PassageSensorDeclaration> sensors,
        IReadOnlyList<ColliderDeclaration>? colliders,
        PhysicsBodyReadSet bodies)
    {
        var sensorList = sensors.ToArray();
        var stateList = new List<PassageBodyState>();

        foreach (var sensor in sensorList)
        {
            var detectorPos = RenderPosition(sensor.Cell, sensor.Local);
            var q = new Quaternion(sensor.Rotation.X, sensor.Rotation.Y, sensor.Rotation.Z, sensor.Rotation.W);
            var qInv = Quaternion.Conjugate(q);

            for (var b = 0; b < bodies.Count; b++)
            {
                var body = bodies[b];
                if (body.Body.Id == sensor.Frame) continue;
                var bodyPos = RenderPosition(body.Body.Cell, body.Body.Local);
                var localPos = Vector3.Transform(bodyPos - detectorPos, qInv);

                var radius = 0.34f;
                var colliderId = new GpuColliderId(body.Body.Id.Value + 1);
                if (colliders != null)
                {
                    foreach (var c in colliders)
                    {
                        if (c.Body == body.Body.Id)
                        {
                            colliderId = c.Id;
                            radius = c.Shape switch
                            {
                                ColliderShapeKind.Sphere => (float)c.Radius.Value,
                                ColliderShapeKind.Box => Math.Max((float)c.HalfExtents.X, Math.Max((float)c.HalfExtents.Y, (float)c.HalfExtents.Z)),
                                ColliderShapeKind.Cylinder => Math.Max((float)c.Radius.Value, (float)c.HalfExtents.Y),
                                ColliderShapeKind.Plane => 0f,
                                _ => throw new ArgumentOutOfRangeException(nameof(colliders), c.Shape, "Undefined collider shape.")
                            };
                            break;
                        }
                    }
                }

                if (radius <= 0f) radius = 0.34f;

                var armed = localPos.X < -(float)sensor.RearmClearance.Value - radius;
                stateList.Add(new PassageBodyState(
                    sensor.Id,
                    body.Body.Id,
                    colliderId,
                    radius,
                    armed ? PassageSensorPhase.Armed : PassageSensorPhase.Unarmed,
                    localPos.X,
                    localPos.Y,
                    localPos.Z,
                    0));
            }
        }

        return new PassageSensorTracker(sensorList, stateList.ToArray());
    }

    public PassageSensorTracker Advance(
        SimulationTick tick,
        PhysicsBodyReadSet bodies,
        PhysicsMotionRead? motion,
        Span<PassageSensorRead> outputs,
        out int outputCount)
    {
        outputCount = 0;
        var nextStates = new PassageBodyState[_states.Length];
        uint substeps = motion != null ? motion.Substeps : 1;

        for (var i = 0; i < _states.Length; i++)
        {
            var current = _states[i];
            var sensor = Array.Find(_sensors, s => s.Id == current.Sensor);
            if (sensor.Id.Value == 0)
            {
                nextStates[i] = current;
                continue;
            }

            var detectorPos = RenderPosition(sensor.Cell, sensor.Local);
            var q = new Quaternion(sensor.Rotation.X, sensor.Rotation.Y, sensor.Rotation.Z, sensor.Rotation.W);
            var qInv = Quaternion.Conjugate(q);

            var bodyIndex = -1;
            for (var b = 0; b < bodies.Count; b++)
            {
                if (bodies[b].Body.Id == current.Body)
                {
                    bodyIndex = b;
                    break;
                }
            }

            if (bodyIndex < 0)
            {
                nextStates[i] = current;
                continue;
            }

            var body = bodies[bodyIndex];
            var radius = current.Radius > 0f ? current.Radius : 0.34f;

            var phase = current.Phase;
            var prevX = current.PreviousLocalX;
            var prevY = current.PreviousLocalY;
            var prevZ = current.PreviousLocalZ;
            var passedCount = current.PassedCount;

            var rearmThreshold = -(float)sensor.RearmClearance.Value - radius;
            var boreRadiusSq = (float)sensor.Radius.Value * (float)sensor.Radius.Value;

            for (uint s = 0; s < substeps; s++)
            {
                var ordinal = motion != null ? (uint)((tick.Value - 1) * substeps + s + 1) : (uint)tick.Value;

                Vector3 bodyPos;
                if (s == substeps - 1 || motion == null)
                {
                    bodyPos = RenderPosition(body.Body.Cell, body.Body.Local);
                }
                else
                {
                    if (motion.TrySample(body.Body.Id, ordinal - 0.0001, out var sampled))
                    {
                        bodyPos = RenderPosition(sampled.Cell, sampled.Local);
                    }
                    else
                    {
                        bodyPos = RenderPosition(body.Body.Cell, body.Body.Local);
                    }
                }

                var localPos = Vector3.Transform(bodyPos - detectorPos, qInv);
                var currX = localPos.X;
                var currY = localPos.Y;
                var currZ = localPos.Z;

                if (currX < rearmThreshold)
                {
                    phase = PassageSensorPhase.Armed;
                }

                if (phase == PassageSensorPhase.Armed && prevX <= 0f && currX > 0f)
                {
                    var denom = currX - prevX;
                    var t = denom > 1e-6f ? (0f - prevX) / denom : 0f;
                    t = Math.Clamp(t, 0f, 1f);

                    var crossY = prevY + t * (currY - prevY);
                    var crossZ = prevZ + t * (currZ - prevZ);
                    var rCrossSq = crossY * crossY + crossZ * crossZ;

                    if (rCrossSq <= boreRadiusSq)
                    {
                        phase = PassageSensorPhase.Unarmed;
                        passedCount++;

                        var dt = 1.0f / WorkshopCadenceSettings.PhysicalFrequency;
                        var speedVal = MathF.Abs(denom / dt);

                        if (outputCount < outputs.Length)
                        {
                            outputs[outputCount++] = new PassageSensorRead(
                                sensor.Id,
                                sensor.Frame,
                                body.Body.Id,
                                current.Collider,
                                PassageSensorPhase.Fired,
                                ordinal,
                                (Half)0,
                                new LinearSpeed((Half)Math.Clamp(speedVal, 0f, 4096f)));
                        }
                    }
                    else
                    {
                        phase = PassageSensorPhase.Unarmed;
                    }
                }

                prevX = currX;
                prevY = currY;
                prevZ = currZ;
            }

            nextStates[i] = new PassageBodyState(
                current.Sensor,
                current.Body,
                current.Collider,
                radius,
                phase,
                prevX,
                prevY,
                prevZ,
                passedCount);
        }

        return new PassageSensorTracker(_sensors, nextStates);
    }

    private static Vector3 RenderPosition(CellOrigin cell, LocalPosition local) =>
        new((cell.X + (float)local.X) / 16f, (cell.Y + (float)local.Y) / 16f, (cell.Z + (float)local.Z) / 16f);
}
