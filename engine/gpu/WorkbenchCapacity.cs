using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>A compiled table whose document invariant an authored population can break. Closed set; display text lives at the
/// exception boundary. Story 9R.5b (owner decision, 20 Oct 2026): no table has a fixed capacity, so the only entry is the guide
/// invariant (one planar guide per moving body).</summary>
public enum WorkbenchTable : uint { Guides }

/// <summary>Explicit rejection when a construction breaks a compiled table's document invariant. Never a fault.</summary>
public sealed class WorkbenchFullException : ArgumentException
{
    public WorkbenchTable Table { get; }
    public WorkbenchFullException(WorkbenchTable table) : base("Workbench is full: " + Describe(table)) => Table = table;
    private static string Describe(WorkbenchTable table) => table switch
    {
        WorkbenchTable.Guides => "ball guide table (one Receiver guides the balls)",
        _ => throw new ArgumentException("Undefined workbench table.")
    };
}

/// <summary>Build sets no population limit: every physics and worker table is sized from the compiled scene at Run, and memory
/// is the only bound (Story 9R.5b). The one rule left is a document invariant, not a capacity: every Receiver declares a planar
/// guide for each ball and the physics document admits one guide per moving body, so a second Receiver fits only while no ball is
/// on the workbench.</summary>
public static class WorkbenchCapacity
{
    public static void Validate(IReadOnlyList<IWorkshopInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);
        var balls = 0; var receivers = 0;
        foreach (var instance in instances)
        {
            if (instance is WorkshopBall) balls++;
            else if (instance.PartId == PartId.Receiver) receivers++;
        }
        if (receivers > 1 && balls > 0) throw new WorkbenchFullException(WorkbenchTable.Guides);
    }
}
