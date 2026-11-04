using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

/// <summary>The one resolver of run-controllable lanes (engine-run-control-table). The validator, the declaration lookup and the
/// compiler all ask it which lanes a declared parameter binds; a RunControl parameter must bind exactly one.</summary>
public static class RunControlBindings
{
    /// <summary>Every run-controllable lane the declaration binds to the parameter <paramref name="key"/>: a Source store's enable
    /// state is the SourceEnable lane.</summary>
    public static IReadOnlyList<RunControlLane> Lanes(BundleEntry entry, string key)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var lanes = new List<RunControlLane>(1);
        if (entry.Electrical?.Supply?.Store is { } store && store.Enable == key) lanes.Add(RunControlLane.SourceEnable);
        return lanes;
    }
}
