using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public readonly record struct BoxProxy(Vector3 At, Vector3 Half);

public partial class MachinePart : Node3D
{
    public PartDefinition Definition { get; set; } = null!;
    public string Uid { get; private set; } = "";
    public bool Locked { get; private set; }
    public List<PartDifficulty> Difficulty { get; private set; } = new();
    public void SetDifficulty(IEnumerable<PartDifficulty> settings) => Difficulty = new(settings);
    public PartDifficulty Assistance(float precision) => PartAssistance.Evaluate(Difficulty, precision);
    public Vector3 Velocity { get; set; }
    public bool Dynamic { get; protected set; }
    public float Radius { get; protected set; } = .32f;
    public float Mass { get; protected set; } = 1;
    public float Bounce { get; protected set; } = .35f;
    public float Drag { get; protected set; } = .04f;
    public float Buoyancy { get; protected set; }
    public List<BoxProxy> Boxes { get; } = new();
    public bool Active { get; set; }
    public Dictionary<string, float> Properties { get; private set; } = new();
    public float PickRadius { get; protected set; } = .65f;
    protected Node3D Visual = null!;
    private MeshInstance3D _highlight = null!;
    public virtual float SurfaceBounce => 1;
    public virtual bool CanSendPower => false;
    public virtual bool CanReceivePower => false;

    public void Configure(PartSpec specification)
    {
        Uid = specification.Id;
        Name = Uid;
        Locked = specification.Locked;
        Difficulty = specification.Difficulty;
        Properties = new();
        foreach (var pair in Definition.Parameters) Properties[pair.Key] = pair.Value;
        foreach (var pair in specification.Properties) Properties[pair.Key] = pair.Value;
        Position = new(specification.Position[0], specification.Position[1], specification.Position[2]);
        RotationDegrees = new(specification.Rotation[0], specification.Rotation[1], specification.Rotation[2]);
    }

    public override void _Ready()
    {
        Visual = new Node3D { Name = "Visual" };
        AddChild(Visual);
        Build();
        _highlight = PartArt.Ring(this, PickRadius, .025f, new("#efffbd"), new(0, 0, .02f));
        _highlight.RotationDegrees = new(90, 0, 0);
        _highlight.Visible = false;
    }
    protected float Parameter(string name, float fallback) => Properties.GetValueOrDefault(name, fallback);
    protected virtual void Build() { }
    protected void AddBox(Vector3 at, Vector3 size, Color color, bool draw = true)
    {
        Boxes.Add(new(at, size * .5f));
        if (draw) PartArt.Box(Visual, size, color, at);
    }
    public virtual void BeforeStep(MachineWorld world, float delta) { }
    public virtual void AfterStep(MachineWorld world, float delta) { }
    public virtual void OnContact(MachinePart body, float speed, MachineWorld world) { }
    public virtual void UpdateAssistance(float precision) { }
    public void SetSelected(bool value) => _highlight.Visible = value;
    public PartSpec Serialize() => new()
    {
        Id = Uid, Kind = Definition.Id, Locked = Locked,
        Position = [Position.X, Position.Y, Position.Z],
        Rotation = [RotationDegrees.X, RotationDegrees.Y, RotationDegrees.Z],
        Properties = new(Properties), Difficulty = Difficulty
    };
}
