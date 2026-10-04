using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Render/input node only. Durable construction and motion are canonical worker values.</summary>
public partial class MachinePart : Node3D
{
    public PartDefinition Definition { get; private set; } = null!;
    public string Uid => Name;
    public Gpu.GpuBodyId AuthoredId { get; internal set; }
    public bool Locked { get; internal set; }
    protected internal Node3D Visual { get; private set; } = null!;
    protected float PickRadius { get; set; }
    private MeshInstance3D? _highlight;
    private bool _built;
    public void Configure(PartDefinition definition)
    {
        if (_built || Definition is not null) throw new InvalidOperationException("Part is already configured.");
        if (definition.WorkshopKind is not (WorkshopPartKind.Basketball or WorkshopPartKind.Receiver or WorkshopPartKind.Ramp) ||
            (definition.WorkshopKind == WorkshopPartKind.Basketball && definition.Basketball is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Ramp && definition.Ramp is null) ||
            definition.Parameters.Count != 0)
            throw new ArgumentException("Unsupported canonical part declaration.");
        if (definition.WorkshopKind == WorkshopPartKind.Basketball) definition.Basketball!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Ramp) definition.Ramp!.Capture();
        Definition = definition;
        Name = definition.Id; // Godot resource/node-name boundary only.
    }
    public override void _Ready() => EnsureConstructed();
    internal void EnsureConstructed()
    {
        if (_built) return;
        if (Definition is null) throw new InvalidOperationException("Part requires an admitted definition.");
        Visual = new Node3D { Name = "Visual" };
        AddChild(Visual);
        Build();
        _highlight = PartArt.Ring(this, PickRadius, .025f, new("#efffbd"), new(0, 0, .02f));
        _highlight.RotationDegrees = new(90, 0, 0);
        _highlight.Visible = false;
        _built = true;
    }
    protected virtual void Build() { }
    public void SetSelected(bool selected)
    {
        if (_highlight is not null) _highlight.Visible = selected;
    }
}
