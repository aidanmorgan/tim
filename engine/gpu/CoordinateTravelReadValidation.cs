using System;

namespace CuriousContraptions.Gpu;

internal static class CoordinateTravelReadValidation
{
    public static void Bodies(WorkshopRead read)
    {
        for (var i = 0; i < read.CoordinateTravelObservers.Count; i++)
        {
            var value = read.CoordinateTravelObservers[i];
            value.Validate();
            if (value.Ordinal > (read.Motion?.LastOrdinal ?? 0))
                throw new ArgumentException("Coordinate travel occurrence is outside committed time.");
        }
    }

    public static void Owners(WorkshopRead read, ICompiledScene scene)
    {
        var declared = scene.Physics.CoordinateTravelObservers;
        if (read.CoordinateTravelObservers.Count != (read.Epoch.Value == 0 ? 0 : declared.Length))
            throw new ArgumentException("Coordinate travel population differs from installation.");
        for (var i = 0; i < read.CoordinateTravelObservers.Count; i++)
        {
            var value = read.CoordinateTravelObservers[i];
            if (value.Id != declared[i].Id || value.Owner != declared[i].Owner || value.Threshold != declared[i].Threshold ||
                read.PhysicalRevision.IsAdmission && (value.Travel != 0 || value.Phase != CoordinateTravelPhase.Accumulating))
                throw new ArgumentException("Coordinate travel observation differs from its declaration or admission.");
        }
    }

    public static void Advance(CoordinateTravelRead current, CoordinateTravelRead previous, uint previousOrdinal)
    {
        current.Validate();
        if (current.Id != previous.Id || current.Owner != previous.Owner || current.Threshold != previous.Threshold ||
            current.Travel < previous.Travel || previous.Phase == CoordinateTravelPhase.Reached && current != previous ||
            current.Phase == CoordinateTravelPhase.Reached && previous.Phase == CoordinateTravelPhase.Accumulating &&
            current.Ordinal <= previousOrdinal)
            throw new ArgumentException("Coordinate travel occurrence changed or predates its committed transition.");
    }

    public static void Advance(WorkshopRead current, WorkshopRead previous)
    {
        if (current.CoordinateTravelObservers.Count != previous.CoordinateTravelObservers.Count)
            throw new ArgumentException("Coordinate travel observation population changed.");
        for (var i = 0; i < current.CoordinateTravelObservers.Count; i++)
            Advance(current.CoordinateTravelObservers[i], previous.CoordinateTravelObservers[i], previous.Motion?.LastOrdinal ?? 0);
    }
}
