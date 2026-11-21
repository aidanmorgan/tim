using System;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{

    private bool ContinuousFeasible(int root, int sources, float dt)
    {
        if (_continuousGraphChanged) return false;
        for (var source = 0; source < sources; source++)
        {
            if (_continuousComponents.SourceRoot(source) != root) continue;
            var work = _continuousWork[source];
            if (!float.IsFinite(work) || work < 0 || work > _continuousReservations[source] ||
                !float.IsFinite(_continuousMechanical[source]) || _continuousMechanical[source] > work) return false;
        }
        foreach (var load in _continuousLoads)
            if (ContinuousLoadRoot(load) == root && load.Impulse > 0 &&
                (!(load.ClosureRate >= 0) || !(load.FinalRate >= 0) || !float.IsFinite(load.Work))) return false;
        foreach (var body in _dynamicBodies)
        {
            if (_continuousComponents.Root(body.BodySlot) != root) continue;
            var slot = body.BodySlot;
            if (!ContinuousVelocityBound(_continuousClosureV[slot], _continuousClosureW[slot],
                    _continuousBaseV[slot], _continuousBaseW[slot], dt) ||
                !ContinuousVelocityBound(body.LinearVelocity, body.AngularVelocity,
                    _continuousBaseFinalV[slot], _continuousBaseFinalW[slot], dt)) return false;
        }
        foreach (var branch in _airflowBranches)
        {
            if (_continuousComponents.SourceRoot(branch.Source) != root || _continuousScales[branch.Source] == 0) continue;
            var receiver = branch.Receiver; var frame = branch.Frame;
            var relative = ConicalAirflow.RelativeSpeed(branch.Point, branch.Axis, receiver.CentreOfMass,
                receiver.LinearVelocity, receiver.AngularVelocity, frame.CentreOfMass, frame.LinearVelocity, frame.AngularVelocity);
            if (!float.IsFinite(relative) || relative > branch.FlowSpeed) return false;
        }
        return true;
    }

    private static bool ContinuousVelocityBound(Vector3 velocity, Vector3 spin, Vector3 baselineV, Vector3 baselineW, float dt)
    {
        // Transfer limits apply to the change induced by the coupled channel, measured against the same ordinary
        // zero-demand trial. Gravity, contact restitution and authored drives keep their existing physical rules.
        return velocity.LengthSquared() <= WorkshopConstraintSolver.MaxLinearSpeed * WorkshopConstraintSolver.MaxLinearSpeed &&
            spin.LengthSquared() <= WorkshopConstraintSolver.MaxAngularSpeed * WorkshopConstraintSolver.MaxAngularSpeed &&
            (velocity - baselineV).LengthSquared() <= (64f * dt) * (64f * dt) &&
            (spin - baselineW).LengthSquared() <= (1024f * dt) * (1024f * dt);
    }

    private void CommitContinuousWork(Span<byte> world, PhysicsWorldLayout layout, float dt)
    {
        for (var source = 0; source < layout.AirflowSources; source++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(source), PhysicsGpuAbi.AirflowSourceBytes);
            var work = _continuousWork[source];
            var kind = (ContinuousWorkInputKind)ReadU32(row, PhysicsGpuAbi.AirflowInputKindOffset);
            if (work > 0 && kind == ContinuousWorkInputKind.Store)
                StorePayment(world, layout.EnergyStoreOffset((int)ReadU32(row, PhysicsGpuAbi.AirflowStoreOffset)), work);
            else if (work > 0 && kind == ContinuousWorkInputKind.Electrical)
                SourcePayment(world, layout.ElectricalSourceOffset((int)ReadU32(row, PhysicsGpuAbi.AirflowElectricalOffset)), work);
            WriteU32(row, PhysicsGpuAbi.AirflowAvailabilityOffset,
                (uint)(_continuousAvailable[source] ? AirflowAvailability.Available : AirflowAvailability.Unavailable));
            WriteF32(row, PhysicsGpuAbi.AirflowObservedSpeedOffset, _continuousAvailable[source] ? ReadF32(row, PhysicsGpuAbi.AirflowSpeedOffset) : 0);
            WriteF32(row, PhysicsGpuAbi.AirflowObservedPowerOffset, work / dt);
            AddEnergy(row, PhysicsGpuAbi.AirflowCommitWorkOffset, work);
            AddEnergy(row, PhysicsGpuAbi.AirflowCommitLossOffset, MathF.Max(0, work - _continuousMechanical[source]));
            WriteF32(row, PhysicsGpuAbi.AirflowWorkOffset, ReadF32(row, PhysicsGpuAbi.AirflowWorkOffset) + work);
            WriteF32(row, PhysicsGpuAbi.AirflowLossOffset,
                ReadF32(row, PhysicsGpuAbi.AirflowLossOffset) + MathF.Max(0, work - _continuousMechanical[source]));
        }
    }

}
