using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private PrismaticTrialWorkspace _continuousTrial = new();
    private readonly List<int> _continuousRoots = [];
    private Vector3[] _continuousBaseV = [], _continuousBaseW = [], _continuousBaseFinalV = [], _continuousBaseFinalW = [];
    private int _continuousStamp;
    private bool _continuousGraphChanged;

    private void SolveContinuousSubstep(Span<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs,
        float dt, SoftCoefficients soft)
    {
        CloseWorkWindow();
        OpenWorkWindow();
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        PrepareBroadphase(dt);
        PrepareContinuousWork(world, layout, inputs);
        _continuousActive = layout.AirflowSources > 0;
        if (!_continuousActive)
        {
            PrepareAllocatedAirflow(dt);
            RunOrdinarySubstep(world, layout, dt, soft);
            return;
        }
        _continuousStamp = _restitutionStamp;
        _continuousTrial.Capture(_dynamicBodies, _allBodies.Count, _joints, _revoluteJoints, _transmissions, _tensile, _candidatePairs);
        EvaluateContinuousTrial(world, layout, dt, soft);
        CaptureContinuousBaseline(layout.Bodies);
        // Each restart adds a dynamic/resource edge. Components only merge, so their population bounds restarts.
        for (var restart = 0; restart <= layout.Bodies + layout.AirflowSources; restart++)
        {
            Array.Clear(_continuousScales);
            foreach (var load in _continuousLoads) load.Impulse = 0;
            _continuousRoots.Clear();
            for (var source = 0; source < layout.AirflowSources; source++)
            {
                var root = _continuousComponents.SourceRoot(source);
                if (_continuousReservations[source] > 0 && !_continuousRoots.Contains(root)) _continuousRoots.Add(root);
            }
            _continuousGraphChanged = false;
            foreach (var root in _continuousRoots)
            {
                SolveContinuousComponent(world, layout, dt, soft, root);
                if (_continuousGraphChanged) break;
            }
            if (_continuousGraphChanged) continue;
            EvaluateContinuousTrial(world, layout, dt, soft);
            if (!_continuousGraphChanged) break;
        }
        // Each selected point was accepted against the complete restored trial. Independent components
        // cannot alter it; newly discovered dynamic edges restart the joint search above. Replay the retained
        // feasible scales/loads once, rather than retiring a payable positive solution after search exhaustion.
        CommitContinuousWork(world, layout, dt);
        _continuousActive = false;
    }

    private void RunOrdinarySubstep(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, float dt, SoftCoefficients soft)
    {
        ApplyGuides(world, layout, dt);
        ApplyCompliantMembranes(world, layout, dt);
        foreach (var body in _dynamicBodies)
        {
            body.Vx += body.Gx * dt; body.Vy += body.Gy * dt; body.Vz += body.Gz * dt;
            var decay = MathF.Exp(-body.Drag * dt);
            body.Vx *= decay; body.Vy *= decay; body.Vz *= decay;
            body.PreSolveKinetic = WorkshopConstraintSolver.Kinetic(body, body.LinearVelocity, body.AngularVelocity);
        }
        SolveBoundedSubstep(dt, soft);
    }

    private void EvaluateContinuousTrial(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, float dt, SoftCoefficients soft)
    {
        GuideWorkspace.Detach(_dynamicBodies);
        _continuousTrial.RestoreState(_dynamicBodies, _joints, _revoluteJoints, _transmissions, _tensile);
        _restitutionStamp = _continuousStamp;
        PrepareAllocatedAirflow(dt);
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        RunOrdinarySubstep(world, layout, dt, soft);
        foreach (var load in _continuousLoads)
        {
            load.Coordinate.Prepare();
            load.FinalRate = load.Sense * load.Coordinate.Rate;
        }
        foreach (var pair in _candidatePairs)
        {
            var a = pair.ColA.Body; var b = pair.ColB.Body;
            if (a.Motion == RigidMotionKind.Dynamic && b.Motion == RigidMotionKind.Dynamic)
                _continuousGraphChanged |= _continuousComponents.Join(a.BodySlot, b.BodySlot);
        }
    }

    private void CaptureContinuousBaseline(int count)
    {
        Scratch(ref _continuousBaseV, count); Scratch(ref _continuousBaseW, count);
        Scratch(ref _continuousBaseFinalV, count); Scratch(ref _continuousBaseFinalW, count);
        foreach (var body in _dynamicBodies)
        {
            var slot = body.BodySlot;
            _continuousBaseV[slot] = _continuousClosureV[slot]; _continuousBaseW[slot] = _continuousClosureW[slot];
            _continuousBaseFinalV[slot] = body.LinearVelocity; _continuousBaseFinalW[slot] = body.AngularVelocity;
        }
    }

    private void SetContinuousScale(int sourceCount, int root, float scale)
    {
        for (var source = 0; source < sourceCount; source++)
            if (_continuousComponents.SourceRoot(source) == root)
                _continuousScales[source] = _airflowDemand[source] > 0
                    ? scale * (_continuousReservations[source] / _airflowDemand[source]) : 0;
    }

    private int ContinuousLoadRoot(ContinuousCoordinateLoad load) =>
        _continuousComponents.Root((load.Coordinate.A.Motion == RigidMotionKind.Dynamic
            ? load.Coordinate.A : load.Coordinate.B).BodySlot);
}
