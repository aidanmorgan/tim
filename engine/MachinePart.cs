using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ActivationDisposition { Immediate, Deferred }

public readonly record struct BoxProxy(Vector3 At, Vector3 Half);
public readonly record struct SphereProxy(Vector3 At, float Radius);

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
    public List<SphereProxy> Spheres { get; } = new();
    public bool Active { get; set; }
    public Dictionary<string, float> Properties { get; private set; } = new();
    public float PickRadius { get; protected set; } = .65f;
    protected Node3D Visual = null!;
    private MeshInstance3D _highlight = null!;
    private readonly HashSet<string> _poweredInputs = new();
    public bool HasElectricalPower(string port) => _poweredInputs.Contains(port);
    internal void ClearElectricalPower() => _poweredInputs.Clear();
    internal void SupplyElectricalPower(string port) => _poweredInputs.Add(port);
    public virtual LightEmitter? LightSource => null;
    public virtual IEnumerable<LightSample> LightSamples => [];
    public virtual void ReceiveLight(float irradiance) { }
    public virtual bool SuppliesElectricity(string outputPort) => false;
    public virtual IEnumerable<ElectricalRoute> ElectricalRoutes => [];
    private readonly Dictionary<string, float> _shaftSpeeds = new();
    public float MechanicalSpeed(string port) => _shaftSpeeds[port];
    internal void ClearMechanicalDrive()
    {
        _shaftSpeeds.Clear();
        foreach (var port in ConnectionPorts)
            if (port.Domain == ConnectionDomain.Mechanical) _shaftSpeeds.Add(port.Id, 0);
    }
    internal void SetMechanicalSpeed(string port, float speed) => _shaftSpeeds[port] = speed;
    public virtual IEnumerable<MechanicalRoute> MechanicalRoutes => [];
    public virtual IEnumerable<MechanicalSource> MechanicalSources => [];
    public virtual void MechanicalStep(MachineWorld world, float delta) { }
    public bool HasOutputSocket => System.Linq.Enumerable.Any(ConnectionPorts,
        p => p.Direction is PortDirection.Output or PortDirection.Bidirectional);
    public virtual RopeAttachmentKind RopeAttachment => RopeAttachmentKind.None;
    public virtual void AdvanceRope(float distance) { }
    public virtual float SurfaceBounce => 1;
    public virtual ActivationDisposition HandleActivation(MachineWorld world) => ActivationDisposition.Immediate;
    public virtual bool CanSendActivation => false;
    public virtual bool CanReceiveActivation => false;

    // Activation is a latched command, not an electrical source.
    // New families expose distinct typed sockets for supply, control and drive.
    public virtual IEnumerable<ConnectionPort> ConnectionPorts
    {
        get
        {
            if (CanSendActivation) yield return new(SocketIds.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, Vector3.Zero);
            if (CanReceiveActivation) yield return new(SocketIds.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, Vector3.Zero);
        }
    }

    public void Configure(PartSpec specification)
    {
        Uid = specification.Id;
        Name = Uid;
        Locked = specification.Locked;
        Difficulty = specification.Difficulty;
        Properties = new();
        foreach (var pair in Definition.Parameters) Properties[pair.Key] = pair.Value;
        foreach (var pair in specification.Properties) Properties[pair.Key] = pair.Value;
        ValidateParameters();
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
    protected void UpdateSelectionRadius(float radius)
    {
        PickRadius = radius;
        if (_highlight?.Mesh is TorusMesh ring)
        {
            ring.OuterRadius = radius + .025f;
            ring.InnerRadius = Mathf.Max(.001f, radius - .025f);
        }
    }
    public virtual void ValidateParameters() { }
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
