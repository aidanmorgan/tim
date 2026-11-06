using System;
using System.Diagnostics;

namespace CuriousContraptions.Gpu;

/// <summary>One stopped collector measurement of one runtime context: managed usage before and after a full blocking collection,
/// the collection's duration in <see cref="Stopwatch"/> ticks, and the allocation and collection counters around it. Usage is not
/// exact live or resident memory.</summary>
public readonly record struct WorkshopMemoryMeasurement(long UsedBefore, long UsedAfter, long DurationTicks, long Frequency,
    long AllocatedBefore, long AllocatedAfter, int CollectionsBefore, int CollectionsAfter)
{
    public void Validate()
    {
        if (UsedBefore < 0 || UsedAfter < 0 || DurationTicks < 0 || Frequency <= 0 ||
            AllocatedBefore < 0 || AllocatedAfter < AllocatedBefore ||
            CollectionsBefore < 0 || CollectionsAfter < CollectionsBefore)
            throw new ArgumentException("Invalid memory observation measurements.");
    }

    /// <summary>Measures this runtime context now (it runs one full blocking collection).</summary>

}

/// <summary>One stopped memory observation: the browser's measurement and the Simulation worker's, taken for one query.</summary>
public readonly record struct WorkshopMemoryObservation(WorkshopMemoryMeasurement Browser, WorkshopMemoryMeasurement Simulation);

/// <summary>The answer to one memory query: its command result, and the observation when the query was applied.</summary>
public readonly record struct WorkshopMemoryReply(WorkshopCommandResult Result, WorkshopMemoryObservation? Observation);
