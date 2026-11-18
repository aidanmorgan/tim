using System;

namespace CuriousContraptions.Gpu;

internal static class ChamberReadValidation
{
    public static void ValidateBodies(WorkshopRead read)
    {
        for (var i = 0; i < read.ChamberRegions.Count; i++)
        {
            var region = read.ChamberRegions[i];
            if ((region.Candidate.Value != 0 && !read.Bodies.TryGet(region.Candidate, out _)) ||
                (region.Departing.Value != 0 && !read.Bodies.TryGet(region.Departing, out _)))
                throw new ArgumentException("Region observation names a foreign dynamic body.");
        }
        for (var i = 0; i < read.FiniteWorkActuators.Count; i++)
            if (read.FiniteWorkActuators[i].LastPayload.Value != 0 && !read.Bodies.TryGet(read.FiniteWorkActuators[i].LastPayload, out _))
                throw new ArgumentException("Finite-work observation names a foreign dynamic body.");
    }

    public static void ValidateOwners(WorkshopRead read, ICompiledScene scene)
    {
        var regions = scene.Physics.ChamberRegions; var actuators = scene.Physics.FiniteWorkActuators;
        if (read.ChamberRegions.Count != (read.Epoch.Value == 0 ? 0 : regions.Length) ||
            read.FiniteWorkActuators.Count != (read.Epoch.Value == 0 ? 0 : actuators.Length))
            throw new ArgumentException("Chamber or finite-work observation population differs from installation.");
        for (var i = 0; i < read.ChamberRegions.Count; i++)
        {
            var value = read.ChamberRegions[i]; var declared = regions[i];
            if (value.Id != declared.Id || value.Frame != declared.Frame ||
                !TargetMatches(value.Candidate, declared.Targets, scene.Physics) || !TargetMatches(value.Departing, declared.Targets, scene.Physics))
                throw new ArgumentException("Region observation differs from its declared owner or targets.");
            if (read.PhysicalRevision.IsAdmission && (value.Phase != ChamberPhase.Empty || value.Occupancy != 0 || value.Departing.Value != 0))
                throw new ArgumentException("Admission carries transient chamber state.");
        }
        for (var i = 0; i < read.FiniteWorkActuators.Count; i++)
            ValidateActuatorOwner(read.FiniteWorkActuators[i], actuators[i], read, scene.Physics);
    }

    private static void ValidateActuatorOwner(FiniteWorkActuatorRead value, FiniteWorkActuatorDeclaration declared,
        WorkshopRead read, IPhysicsSceneDeclaration physics)
    {
        if (value.Id != declared.Id || value.Store != declared.Store || value.Region != declared.Region)
            throw new ArgumentException("Finite-work observation differs from its declared references.");
        foreach (var region in physics.ChamberRegions)
            if (region.Id == declared.Region && !TargetMatches(value.LastPayload, region.Targets, physics))
                throw new ArgumentException("Finite-work observation names a foreign target.");
        for (var regionIndex = 0; regionIndex < read.ChamberRegions.Count; regionIndex++)
            if (read.ChamberRegions[regionIndex].Id == value.Region &&
                ((read.ChamberRegions[regionIndex].Phase == ChamberPhase.Seated) == (value.Clearance == FiniteWorkClearance.NotApplicable)))
                throw new ArgumentException("Finite-work clearance differs from current chamber eligibility.");
        if (read.PhysicalRevision.IsAdmission && (value.LastResult is not null || value.OccurrenceCount != 0 ||
            value.Clearance != FiniteWorkClearance.NotApplicable))
            throw new ArgumentException("Admission carries transient finite-work state.");
    }

    public static void ValidateAdvance(WorkshopRead current, WorkshopRead previous)
    {
        if (current.ChamberRegions.Count != previous.ChamberRegions.Count || current.FiniteWorkActuators.Count != previous.FiniteWorkActuators.Count)
            throw new ArgumentException("Region or finite-work population changed.");
        for (var i = 0; i < current.ChamberRegions.Count; i++)
            if (current.ChamberRegions[i].Id != previous.ChamberRegions[i].Id || current.ChamberRegions[i].Frame != previous.ChamberRegions[i].Frame)
                throw new ArgumentException("Region ownership changed.");
        for (var i = 0; i < current.FiniteWorkActuators.Count; i++)
        {
            var a = current.FiniteWorkActuators[i]; var b = previous.FiniteWorkActuators[i];
            if (a.Id != b.Id || a.Store != b.Store || a.Region != b.Region ||
                a.OccurrenceCount < b.OccurrenceCount || (ulong)a.OccurrenceCount > (ulong)b.OccurrenceCount + 1UL ||
                (a.OccurrenceCount != b.OccurrenceCount && a.LastResult != FiniteWorkRequestResult.Fired))
                throw new ArgumentException("Finite-work identity or coalesced occurrence changed illegally.");
        }
    }

    private static bool TargetMatches(GpuBodyId id, BodyTargetSet targets, IPhysicsSceneDeclaration scene)
    {
        if (id.Value == 0) return true;
        foreach (var body in scene.Bodies)
            if (body.Id == id) return body.Motion == RigidMotionKind.Dynamic && targets.Contains(id, body.Payload);
        return false;
    }
}
