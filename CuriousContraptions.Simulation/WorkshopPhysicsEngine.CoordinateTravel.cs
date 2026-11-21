using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private void CommitCoordinateTravel(Span<byte> world, PhysicsWorldLayout layout, float seconds, uint ordinal)
    {
        for (var i = 0; i < layout.CoordinateTravelObservers; i++)
        {
            var row = world.Slice(layout.CoordinateTravelOffset(i), PhysicsGpuAbi.CoordinateTravelBytes);
            if ((CoordinateTravelPhase)ReadU32(row, PhysicsGpuAbi.CoordinateTravelPhaseOffset) == CoordinateTravelPhase.Reached) continue;
            var joint = _revoluteJoints[(int)ReadU32(row, PhysicsGpuAbi.CoordinateTravelCoordinateOffset)];
            var axis = joint.Rows[(int)RevoluteRowKind.Drive];
            var rate = ConstraintBodyImpulse.ConstraintVelocity(joint.A, axis.La, axis.Aa) +
                ConstraintBodyImpulse.ConstraintVelocity(joint.B, axis.Lb, axis.Ab);
            var threshold = ReadF32(row, PhysicsGpuAbi.CoordinateTravelThresholdOffset);
            var travel = MathF.Min(threshold, ReadF32(row, PhysicsGpuAbi.CoordinateTravelAccumulatedOffset) + MathF.Abs(rate * seconds));
            WriteF32(row, PhysicsGpuAbi.CoordinateTravelAccumulatedOffset, travel);
            if (travel < threshold) continue;
            WriteU32(row, PhysicsGpuAbi.CoordinateTravelPhaseOffset, (uint)CoordinateTravelPhase.Reached);
            WriteU32(row, PhysicsGpuAbi.CoordinateTravelOrdinalOffset, ordinal);
        }
    }
}
