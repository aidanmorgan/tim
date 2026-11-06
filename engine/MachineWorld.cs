using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Godot presentation boundary for the current GPU Workshop.</summary>
public partial class MachineWorld : Node3D
{
    public const int Substeps = 4;
    public ICoreServices Core { get; init; } = null!;
    public IBundleServices Bundle { get; init; } = null!;
    public PlacementPose Placement { get; init; } = null!;
    public PartRegistry Registry { get; init; } = null!;
    private readonly List<MachinePart> _parts = new(16);
    private readonly List<MachinePart> _bodies = new(16);
    public IReadOnlyList<MachinePart> Parts { get; }
    public IReadOnlyList<MachinePart> Bodies { get; }
    public bool Running { get; private set; }
    public int Ticks { get; private set; }
    public PhysicalTime DisplaySimulationTime { get; private set; }
    public MachineWorld()
    {
        Parts = _parts.AsReadOnly();
        Bodies = _bodies.AsReadOnly();
    }
    public override void _Process(double delta) => PresentWorkshopRead();
    public override async void _ExitTree() => await DisposeWorkshop();
}
