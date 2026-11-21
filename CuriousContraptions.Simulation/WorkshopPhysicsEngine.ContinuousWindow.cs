using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>One opposing load per exact coordinate, shared by every source that consumes it.</summary>
internal sealed class ContinuousCoordinateLoad
{
    public readonly SimConstraintCoordinate Coordinate = new();
    public int Slot;
    public float Sense, StartRate, Demand, Target, Impulse, Work, ClosureRate, FinalRate;

    public ImpulseJacobian Row => new(Coordinate.A, Coordinate.Row.La, Coordinate.Row.Aa,
        Coordinate.B, Coordinate.Row.Lb, Coordinate.Row.Ab);
}

internal sealed partial class WorkshopPhysicsEngine
{
    private readonly List<ContinuousCoordinateLoad> _continuousLoads = [];
    private float[] _continuousScales = [], _continuousWork = [], _continuousMechanical = [];
    private Vector3[] _continuousClosureV = [], _continuousClosureW = [];
    private bool _continuousActive;

    private static ImpulseJacobian AirflowRow(in AirflowBranch branch) =>
        new(branch.Receiver, branch.Axis, Vector3.Cross(branch.Point - branch.Receiver.CentreOfMass, branch.Axis),
            branch.Frame, -branch.Axis, Vector3.Cross(branch.Point - branch.Frame.CentreOfMass, -branch.Axis));

    private void ApplyContinuousImpulses()
    {
        if (!_continuousActive) return;
        Array.Clear(_continuousWork);
        Array.Clear(_continuousMechanical);
        foreach (var branch in _airflowBranches) AirflowRow(branch).Apply(branch.Impulse * _continuousScales[branch.Source]);
        foreach (var load in _continuousLoads)
        {
            load.Coordinate.Prepare();
            load.Row.Apply(-load.Sense * load.Impulse);
        }
        foreach (var body in _dynamicBodies)
            body.PreSolveKinetic = WorkshopConstraintSolver.Kinetic(body, body.LinearVelocity, body.AngularVelocity);
    }

    private void MeasureContinuousWindow()
    {
        if (!_continuousActive) return;
        foreach (var branch in _airflowBranches)
        {
            var impulse = branch.Impulse * _continuousScales[branch.Source];
            var row = AirflowRow(branch);
            var work = 0f;
            for (var i = 0; i < row.Count; i++)
            {
                row.Get(i, out var body, out var linear, out var angular);
                if (body.Motion != RigidMotionKind.Dynamic) continue;
                work += impulse * (Vector3.Dot(linear, .5f * body.WindowV + .5f * body.LinearVelocity) +
                    Vector3.Dot(angular, .5f * body.WindowW + .5f * body.AngularVelocity));
            }
            _continuousMechanical[branch.Source] += work;
            _continuousWork[branch.Source] += branch.FlowSpeed * impulse;
        }
        foreach (var load in _continuousLoads)
        {
            load.ClosureRate = load.Sense * load.Coordinate.Rate;
            load.Work = load.Impulse * (.5f * load.StartRate + .5f * load.ClosureRate);
        }
        foreach (var body in _dynamicBodies)
        {
            _continuousClosureV[body.BodySlot] = body.LinearVelocity;
            _continuousClosureW[body.BodySlot] = body.AngularVelocity;
        }
    }
}
