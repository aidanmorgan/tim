using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private sealed class RotaryCapture
    {
        public SimBody Frame = null!;
        public SimRevoluteJoint Joint = null!;
        public readonly SimConstraintCoordinate Coordinate = new();
        public float PitchLimit, Response, CutIn, MaximumRate, StartRate;
        public readonly List<AirflowBranch> Branches = [];
        public RotaryAirflowBranch[] Frozen = [];
    }
    private readonly List<RotaryCapture> _rotaryCapturePool = [];
    private readonly List<RotaryCapture> _rotaryCaptures = [];

    private void GatherRotaryCaptures(ReadOnlySpan<byte> world, PhysicsWorldLayout layout)
    {
        _rotaryCaptures.Clear();
        for (var i = 0; i < layout.RotaryAirflowCaptures; i++)
        {
            if (i == _rotaryCapturePool.Count) _rotaryCapturePool.Add(new());
            var capture = _rotaryCapturePool[i];
            var row = world.Slice(layout.RotaryAirflowCaptureOffset(i), PhysicsGpuAbi.RotaryAirflowCaptureBytes);
            capture.Frame = _allBodies[(int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowFrameOffset)];
            capture.Joint = _revoluteJoints[(int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowCoordinateOffset)];
            capture.Coordinate.Bind(capture.Joint); capture.Coordinate.Prepare();
            capture.StartRate = capture.Coordinate.Rate;
            capture.PitchLimit = ReadF32(row, PhysicsGpuAbi.RotaryAirflowPitchOffset);
            capture.Response = ReadF32(row, PhysicsGpuAbi.RotaryAirflowResponseOffset);
            capture.CutIn = ReadF32(row, PhysicsGpuAbi.RotaryAirflowCutInOffset);
            capture.MaximumRate = ReadF32(row, PhysicsGpuAbi.RotaryAirflowMaximumRateOffset);
            capture.Branches.Clear();
            _rotaryCaptures.Add(capture);
        }
    }

    private void GatherRotaryBranches(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, ReadOnlySpan<byte> sourceRow,
        int source, SimBody nozzle, Vector3 origin, Vector3 axis, float flow, float pressure, float seconds)
    {
        for (var i = 0; i < _rotaryCaptures.Count; i++)
        {
            var capture = _rotaryCaptures[i];
            var row = world.Slice(layout.RotaryAirflowCaptureOffset(i), PhysicsGpuAbi.RotaryAirflowCaptureBytes);
            var first = (int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowSampleStartOffset);
            var count = (int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowSampleCountOffset);
            for (var sample = 0; sample < count; sample++)
            {
                var offset = layout.AirflowSampleOffset(first + sample);
                var point = capture.Frame.Position + Vector3.Transform(ReadVector(world, offset), capture.Frame.Rotation);
                if (!ConicalAirflow.Contains(point - origin, axis, ReadF32(sourceRow, PhysicsGpuAbi.AirflowReachOffset),
                        ReadF32(sourceRow, PhysicsGpuAbi.AirflowRadiusOffset)) ||
                    !AirflowLineClear(world, layout, sourceRow, -1,
                        point - axis * Vector3.Dot(point - origin, axis), point,
                        (int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowExclusionStartOffset),
                        (int)ReadU32(row, PhysicsGpuAbi.RotaryAirflowExclusionCountOffset))) continue;
                var velocity = ConicalAirflow.RelativeSpeed(point, axis, capture.Frame.CentreOfMass,
                    capture.Frame.LinearVelocity, capture.Frame.AngularVelocity,
                    nozzle.CentreOfMass, nozzle.LinearVelocity, nozzle.AngularVelocity);
                var force = ReadF32(sourceRow, PhysicsGpuAbi.AirflowForceOffset);
                var weight = ReadF32(world, offset + 12);
                var conductance = force * weight * pressure / flow;
                var impulse = ConicalAirflow.Impulse(force, flow, velocity, weight, pressure, seconds);
                if (!(impulse > 0)) continue;
                var branch = AddAirflowBranch(source, nozzle, capture.Frame, point, axis, flow, impulse);
                branch.RelativeVelocity = velocity; branch.Conductance = conductance;
                branch.Alignment = Math.Clamp(Vector3.Dot(axis, capture.Coordinate.Row.Ab), -1, 1);
                branch.PrepareRow(0, capture.Coordinate);
                capture.Branches.Add(branch);
            }
        }
    }

    private void PrepareAllocatedAirflow(float seconds)
    {
        foreach (var branch in _airflowBranches)
            branch.PreparedImpulse = branch.Impulse * _continuousScales[branch.Source];
        foreach (var capture in _rotaryCaptures)
        {
            var frozen = Scratch(ref capture.Frozen, capture.Branches.Count);
            for (var i = 0; i < frozen.Length; i++)
            {
                var branch = capture.Branches[i];
                var scale = _continuousScales[branch.Source];
                frozen[i] = new(branch.FlowSpeed, branch.RelativeVelocity, branch.Conductance * scale,
                    branch.Impulse * scale / seconds, branch.Alignment);
            }
            var response = RotaryAirflowLaw.Prepare(frozen, capture.PitchLimit, capture.Response,
                capture.CutIn, capture.MaximumRate);
            capture.Joint.Damping = response.Resistance;
            capture.Joint.Soft = capture.Joint.Stiffness > 0
                ? SoftConstraintCoefficients.FromSpring(capture.Joint.Stiffness, response.Resistance, seconds)
                : response.Resistance > 0 ? SoftConstraintCoefficients.FromDamper(response.Resistance, seconds) : default;
            for (var i = 0; i < frozen.Length; i++)
            {
                var branch = capture.Branches[i];
                branch.PreparedImpulse = response.Pitch == 0 ? 0 : frozen[i].Force(response.Pitch, capture.StartRate) * seconds;
                branch.PrepareRow(response.Pitch, capture.Coordinate);
            }
        }
    }
}
