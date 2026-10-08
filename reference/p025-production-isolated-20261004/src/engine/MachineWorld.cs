using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Godot presentation boundary for the current GPU Workshop.</summary>
public partial class MachineWorld : Node3D
{
    public const int Substeps = 4;
    public PartRegistry Registry { get; } = new();
    private readonly List<MachinePart> _parts = new();
    private readonly List<MachinePart> _bodies = new();
    public IReadOnlyList<MachinePart> Parts { get; }
    public IReadOnlyList<MachinePart> Bodies { get; }
    public bool Running { get; private set; }
    public int Ticks { get; private set; }
    public double DisplaySimulationTime { get; private set; }
    public MachineWorld()
    {
        Parts = _parts.AsReadOnly();
        Bodies = _bodies.AsReadOnly();
    }
    public override void _Ready() => Registry.Discover();
    public override void _Process(double delta) => PresentWorkshopRead();
    public override async void _ExitTree() => await DisposeWorkshop();
}
