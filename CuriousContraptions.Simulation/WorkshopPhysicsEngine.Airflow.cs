using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private sealed class AirflowBranch
    {
        public int Source;
        public SimBody Frame = null!, Receiver = null!;
        public Vector3 Point, Axis;
        public float FlowSpeed, Impulse, PreparedImpulse, Conductance, RelativeVelocity, Alignment;
        private readonly SimBody[] _bodies = new SimBody[4];
        private readonly Vector3[] _linear = new Vector3[4], _angular = new Vector3[4];
        private int _count;
        public ImpulseJacobian Row => new(_bodies.AsSpan(0, _count), _linear.AsSpan(0, _count), _angular.AsSpan(0, _count));

        public void PrepareRow(float pitch, SimConstraintCoordinate? coordinate)
        {
            _count = 0;
            Add(Receiver, Axis, Vector3.Cross(Point - Receiver.CentreOfMass, Axis));
            Add(Frame, -Axis, Vector3.Cross(Point - Frame.CentreOfMass, -Axis));
            if (coordinate is null) return;
            var effort = pitch * Alignment;
            Add(coordinate.A, effort * coordinate.Row.La, effort * coordinate.Row.Aa);
            Add(coordinate.B, effort * coordinate.Row.Lb, effort * coordinate.Row.Ab);
        }

        private void Add(SimBody body, Vector3 linear, Vector3 angular)
        {
            for (var i = 0; i < _count; i++)
                if (_bodies[i] == body) { _linear[i] += linear; _angular[i] += angular; return; }
            _bodies[_count] = body; _linear[_count] = linear; _angular[_count++] = angular;
        }

        public float Rate()
        {
            var rate = 0f;
            for (var i = 0; i < _count; i++)
                if (_bodies[i].Motion == RigidMotionKind.Dynamic)
                    rate += Vector3.Dot(_linear[i], _bodies[i].LinearVelocity) +
                        Vector3.Dot(_angular[i], _bodies[i].AngularVelocity);
            return rate;
        }
    }
    private List<AirflowBranch> _airflowBranches = [];
    private readonly List<AirflowBranch> _airflowBranchPool = [];

    private AirflowBranch AddAirflowBranch(int source, SimBody frame, SimBody receiver, Vector3 point,
        Vector3 axis, float flow, float impulse)
    {
        var index = _airflowBranches.Count;
        if (index == _airflowBranchPool.Count) _airflowBranchPool.Add(new());
        var branch = _airflowBranchPool[index];
        branch.Source = source; branch.Frame = frame; branch.Receiver = receiver;
        branch.Point = point; branch.Axis = axis; branch.FlowSpeed = flow;
        branch.Impulse = impulse; branch.PreparedImpulse = 0;
        branch.Conductance = branch.RelativeVelocity = branch.Alignment = 0;
        branch.PrepareRow(0, null);
        _airflowBranches.Add(branch);
        return branch;
    }
    private float[] _airflowImpulses = [];

    private void GatherAirflowBranches(ReadOnlySpan<byte> world, PhysicsWorldLayout layout,
        PhysicsTickInputs inputs, float seconds)
    {
        _airflowBranches.Clear();
        GatherRotaryCaptures(world, layout);
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        for (var source = 0; source < layout.AirflowSources; source++)
        {
            if (!inputs.AirflowEnabled[source]) continue;
            var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
            var flow = ReadF32(row, PhysicsGpuAbi.AirflowSpeedOffset);
            var pressure = ReadF32(row, PhysicsGpuAbi.AirflowPressureOffset);
            if (flow == 0 || pressure == 0) continue;
            var frame = _allBodies[(int)ReadU32(row, PhysicsGpuAbi.AirflowFrameOffset)];
            var origin = frame.Position + Vector3.Transform(ReadVector(row, PhysicsGpuAbi.AirflowOriginOffset), frame.Rotation);
            var axis = Vector3.Transform(ReadVector(row, PhysicsGpuAbi.AirflowAxisOffset), frame.Rotation);
            var start = _airflowBranches.Count;
            GatherSourceBranches(world, layout, row, source, frame, origin, axis, flow, pressure, seconds);
            GatherRotaryBranches(world, layout, row, source, frame, origin, axis, flow, pressure, seconds);
            var count = _airflowBranches.Count - start;
            var impulses = Scratch(ref _airflowImpulses, count);
            for (var i = 0; i < count; i++) impulses[i] = _airflowBranches[start + i].Impulse;
            ConicalAirflow.LimitSource(impulses, ReadF32(row, PhysicsGpuAbi.AirflowForceOffset),
                flow, ReadF32(row, PhysicsGpuAbi.AirflowPowerOffset), seconds);
            for (var i = 0; i < count; i++)
            {
                var branch = _airflowBranches[start + i];
                if (branch.Impulse > 0) branch.Conductance *= impulses[i] / branch.Impulse;
                branch.Impulse = impulses[i];
            }
        }
    }

    private void GatherSourceBranches(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, ReadOnlySpan<byte> sourceRow,
        int source, SimBody frame, Vector3 origin, Vector3 axis, float flow, float pressure, float seconds)
    {
        for (var receiver = 0; receiver < layout.AirflowReceivers; receiver++)
        {
            var row = world.Slice(layout.AirflowReceiverOffset(receiver), PhysicsGpuAbi.AirflowReceiverBytes);
            var colliderSlot = (int)ReadU32(row, PhysicsGpuAbi.AirflowReceiverColliderOffset);
            var collider = _colliderPool[colliderSlot];
            var sampleStart = (int)ReadU32(row, PhysicsGpuAbi.AirflowSampleStartOffset);
            var sampleCount = (int)ReadU32(row, PhysicsGpuAbi.AirflowSampleCountOffset);
            var normalized = (AirflowSampling)ReadU32(row, PhysicsGpuAbi.AirflowSamplingOffset) == AirflowSampling.ColliderNormalized;
            var extent = normalized ? AirflowSampleExtent(collider) : Vector3.One;
            for (var sample = 0; sample < sampleCount; sample++)
            {
                var offset = layout.AirflowSampleOffset(sampleStart + sample);
                var local = ReadVector(world, offset) * extent;
                var point = collider.Position + Vector3.Transform(local, collider.Rotation);
                if (!ConicalAirflow.Contains(point - origin, axis, ReadF32(sourceRow, PhysicsGpuAbi.AirflowReachOffset),
                        ReadF32(sourceRow, PhysicsGpuAbi.AirflowRadiusOffset)) ||
                    !AirflowLineClear(world, layout, sourceRow, colliderSlot,
                        point - axis * Vector3.Dot(point - origin, axis), point)) continue;
                var body = collider.Body;
                var speed = ConicalAirflow.RelativeSpeed(point, axis, body.CentreOfMass, body.LinearVelocity,
                    body.AngularVelocity, frame.CentreOfMass, frame.LinearVelocity, frame.AngularVelocity);
                var impulse = ConicalAirflow.Impulse(ReadF32(sourceRow, PhysicsGpuAbi.AirflowForceOffset),
                    flow, speed, ReadF32(world, offset + 12), pressure, seconds);
                if (impulse > 0) AddAirflowBranch(source, frame, body, point, axis, flow, impulse);
            }
        }
    }

    private static Vector3 AirflowSampleExtent(SimCollider collider)
    {
        var shape = WorkshopCollisionKernels.Partner(collider);
        return new(Vector3.Dot(MaterialSupport.Point(shape, collider.U0) - collider.Position, collider.U0),
            Vector3.Dot(MaterialSupport.Point(shape, collider.U1) - collider.Position, collider.U1),
            Vector3.Dot(MaterialSupport.Point(shape, collider.U2) - collider.Position, collider.U2));
    }

    private bool AirflowLineClear(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, ReadOnlySpan<byte> source,
        int receiver, Vector3 origin, Vector3 point, int receiverExclusionStart = 0, int receiverExclusionCount = 0)
    {
        // A zero-radius sphere is a point surface. The shared continuous material query traces the whole segment,
        // including hollow walls, with the same contact-separation tolerance as other material queries.
        var ray = new HollowPartner(CuriousContraptions.Gpu.ColliderShapeKind.Sphere, origin, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, 0, 0, 0, 0);
        var first = (int)ReadU32(source, PhysicsGpuAbi.AirflowExclusionStartOffset);
        var count = (int)ReadU32(source, PhysicsGpuAbi.AirflowExclusionCountOffset);
        foreach (var obstacle in _colliders)
        {
            if (obstacle.Index == receiver || obstacle.Participation == CollisionParticipation.Disabled) continue;
            var excluded = false;
            for (var i = 0; i < count; i++)
                excluded |= ReadU32(world, layout.AirflowExclusionOffset(first + i)) == obstacle.Index;
            for (var i = 0; i < receiverExclusionCount; i++)
                excluded |= ReadU32(world, layout.AirflowExclusionOffset(receiverExclusionStart + i)) == obstacle.Index;
            if (excluded) continue;
            if (_materialQueries.SweepMaterial(ray, WorkshopCollisionKernels.Partner(obstacle), point - origin) != MaterialQueryResult.Clear)
                return false;
        }
        return true;
    }
}
