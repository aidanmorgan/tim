using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private HashSet<PhysicalRequestTarget> _requestTargets = new(4);

    private void PreparePhysicalRequests(PhysicsTickInputs inputs, ReadOnlySpan<byte> world, PhysicsWorldLayout layout)
    {
        if (inputs.PhysicalRequests is null) throw new ArgumentException("Missing physical request collection.");
        _requestTargets.Clear();
        foreach (var request in inputs.PhysicalRequests)
        {
            request.Occurrence.Validate();
            if (!request.Target.IsValid || !Enum.IsDefined(request.Operation) || !_requestTargets.Add(request.Target))
                throw new ArgumentException("Invalid or duplicate physical request.");
            var exists = request.Target.Kind switch
            {
                PhysicalRequestTargetKind.Retention => HasRetention(request.Target.Retention),
                PhysicalRequestTargetKind.FiniteWorkActuator => FiniteWorkSlot(world, layout, request.Target.Actuator) >= 0,
                _ => false
            };
            if (!exists) throw new ArgumentException("Unknown physical request target.");
        }
        foreach (var request in inputs.PhysicalRequests)
        {
            if (request.Target.Kind != PhysicalRequestTargetKind.Retention) continue;
            foreach (var transmission in _transmissions)
                if (transmission.Retention?.Id == request.Target.Retention) transmission.Retention.Request();
        }
    }

    private bool HasRetention(PrismaticRetentionId id)
    {
        foreach (var transmission in _transmissions)
            if (transmission.Retention?.Id == id) return true;
        return false;
    }

    private static int FiniteWorkSlot(ReadOnlySpan<byte> world, PhysicsWorldLayout layout, FiniteWorkActuatorId id)
    {
        for (var index = 0; index < layout.FiniteWorkActuators; index++)
            if (BinaryPrimitives.ReadUInt64LittleEndian(world[layout.FiniteWorkActuatorOffset(index)..]) == id.Value) return index;
        return -1;
    }
}
