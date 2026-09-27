using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum WoundSpringParameter { Stiffness, Stroke, WindingLead }
public enum WoundSpringPhase { Idle, Winding, Armed, Releasing, Blocked }

/// <summary>Finite-work spring launcher with an anchored, one-way guided plunger.
/// No ammunition is created. The plunger is a real finite-mass internal physics body.</summary>
public partial class WoundSpringPart : MachinePart
{
    public const string CatalogId = "wound_spring";
    public const float RestHeadY = .9f;
    public const float PlungerRadius = .32f;
    public const float PlungerMass = .5f;
    private LatchedSpringStore _spring = null!;
    private GuidedPlunger _head = null!;
    private MeshInstance3D _coil = null!, _chargeMarker = null!;
    private Node3D _pulley = null!, _latch = null!;
    private bool _started;
    private Transform3D _previousPose;
    private readonly List<MachinePart> _internalBodies = new();
    private int? _triggerTick;
    private float _shaftAngle;
    private float _lastHeadY;
    public WoundSpringPhase Phase { get; private set; }
    public SpringTriggerResult? LastTrigger { get; private set; }
    public int ReleaseCount { get; private set; }
    public float LatchAngle => _latch.Rotation.Z;
    public double StoredEnergy => _spring.Energy;
    public double AcceptedWork => _spring.AcceptedWork;
    public double ReleasedWork => _spring.ReleasedWork;
    public float Compression => (float)_spring.Compression;
    public MachinePart Plunger => _head;
    public override IReadOnlyList<MachinePart> InternalBodies => _internalBodies;
    public override IReadOnlyList<InternalBodyRole> InternalBodyRoles => [InternalBodyRole.Plunger];
    public override IEnumerable<SocketId> MechanicalLoads => [SocketId.DriveIn];
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, new(-.62f, -.55f, .55f)),
        new(SocketId.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, new(.62f, -.55f, .55f))
    ];
    public override void ValidateParameters()
    {
        var stiffness = ReadParameter(WoundSpringParameter.Stiffness);
        var stroke = ReadParameter(WoundSpringParameter.Stroke);
        var lead = ReadParameter(WoundSpringParameter.WindingLead);
        if (!float.IsFinite(stiffness) || stiffness < 40 || stiffness > 240)
            throw new ArgumentException("Wound spring stiffness must be between 40 and 240.");
        if (!float.IsFinite(stroke) || stroke < .2f || stroke > .8f)
            throw new ArgumentException("Wound spring stroke must be between 0.2 and 0.8.");
        if (!float.IsFinite(lead) || lead < .025f || lead > .2f)
            throw new ArgumentException("Winding lead must be between 0.025 and 0.2 per radian.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported spring command.");
        _triggerTick ??= world.Ticks;
        return ActivationDisposition.Deferred;
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        if (!_started) { _head.SetCoordinate(RestHeadY); _previousPose = Transform; }
        _started = true;
        if (_triggerTick is { } tick && tick < world.Ticks)
        {
            _triggerTick = null;
            LastTrigger = _spring.Release();
            if (LastTrigger == SpringTriggerResult.Released)
            {
                _head.AtStop = false;
                ReleaseCount++;
                Phase = WoundSpringPhase.Releasing;
            }
        }
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (Transform != _previousPose)
        {
            var local = _previousPose.AffineInverse() * _head.Position;
            _head.Position = Transform * local;
            _head.Velocity = Basis * _previousPose.Basis.Inverse() * _head.Velocity;
            _previousPose = Transform;
        }
        _lastHeadY = _head.Coordinate;
        if (_spring.State != SpringLatchState.Releasing) return;
        _head.Velocity += _head.Axis * ((float)_spring.Force / PlungerMass * delta);
        _head.ConstrainVelocity();
    }
    public override void MechanicalStep(MachineWorld world, float delta)
    {
        var travel = MechanicalSpeed(SocketId.DriveIn) * delta;
        _shaftAngle = Mathf.PosMod(_shaftAngle + travel, Mathf.Tau);
        _pulley.Rotation = new(0, 0, -_shaftAngle);
        if (_spring.State != SpringLatchState.Latched) return;
        var clearance = ReadParameter(WoundSpringParameter.Stroke);
        if (travel > 0)
        {
            var displacement = -_head.Axis * Mathf.Min(clearance, travel * ReadParameter(WoundSpringParameter.WindingLead));
            var hit = WorldGeometry.Sweep(world, _head.Position, PlungerRadius, displacement, this);
            clearance = hit.Status switch
            {
                SphereSweepStatus.Clear => displacement.Length(),
                SphereSweepStatus.Contact => hit.Distance,
                SphereSweepStatus.Overlapping => 0,
                _ => throw new InvalidOperationException("Unknown winding clearance.")
            };
        }
        var allowedTorque = MechanicalTorque(SocketId.DriveIn);
        var torque = (float)Math.Min(float.MaxValue, allowedTorque);
        if (torque > allowedTorque) torque = MathF.BitDecrement(torque);
        var winding = _spring.Wind(travel, torque, clearance, MechanicalWorkAvailable(SocketId.DriveIn));
        ConsumeMechanicalWork(SocketId.DriveIn, winding.Work);
        _head.SetCoordinate(RestHeadY - Compression);
        Phase = winding.Status == SpringWindStatus.Blocked ? WoundSpringPhase.Blocked :
            winding.Status == SpringWindStatus.Wound ? WoundSpringPhase.Winding :
            Compression > 0 ? WoundSpringPhase.Armed : WoundSpringPhase.Idle;
        UpdateArt();
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        if (_spring.State == SpringLatchState.Releasing)
        {
            var travel = Mathf.Max(0, _head.Coordinate - _lastHeadY);
            _spring.Extend(travel);
            if (_head.AtStop || _spring.State == SpringLatchState.Spent)
            {
                _head.ReachMotionLimit();
                _spring.Extend(float.MaxValue);
                _spring.Latch();
                Phase = WoundSpringPhase.Idle;
            }
            else Phase = travel > .000001f ? WoundSpringPhase.Releasing : WoundSpringPhase.Blocked;
        }
        Active = Phase is WoundSpringPhase.Winding or WoundSpringPhase.Releasing;
        UpdateArt();
    }
    public override void _Process(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!_started) _head.SetCoordinate(RestHeadY);
        var latchTarget = _spring.State == SpringLatchState.Releasing ? -.65f : 0;
        _latch.Rotation = new(0, 0, Mathf.MoveToward(LatchAngle, latchTarget, (float)Math.Min(delta * 8, .65)));
        UpdateArt();
    }
    private void UpdateArt()
    {
        var height = _head.Coordinate - PlungerRadius + .8f;
        _coil.Scale = new(1, Mathf.Max(.1f, height) / 1.38f, 1);
        _chargeMarker.Position = new(.6f, _head.Coordinate - .1f, .14f);
    }
    protected override void Build()
    {
        ClearMechanicalDrive(); PickRadius = 1.25f;
        _spring = new(ReadParameter(WoundSpringParameter.Stiffness),
            ReadParameter(WoundSpringParameter.Stroke), ReadParameter(WoundSpringParameter.WindingLead));
        AddBox(new(0, -1, 0), new(1.55f, .2f, 1.35f), new("#293954"));
        foreach (var x in new[] { -.6f, .6f })
            AddBox(new(x, -.05f, 0), new(.13f, 1.8f, .2f), new("#fff8e9"));
        for (var mark = 0; mark <= 4; mark++)
            PartArt.Box(Visual, new(.12f, .025f, .025f), new("#293954"),
                new(.6f, RestHeadY - .1f - ReadParameter(WoundSpringParameter.Stroke) * mark / 4, .112f));
        _chargeMarker = PartArt.Box(Visual, new(.19f, .045f, .035f), new("#f7cb52"));
        // Fixed guide sleeve holds an incoming ball above the physical rounded head.
        var pose = new Transform3D(new Basis(Vector3.Back, Mathf.Pi / 2), new(0, .5f, 0));
        Tubes.Add(new(pose, .9f, .41f, .49f, false));
        PipeArt.Cylinder(Visual, pose, .9f, .41f, .49f, new Color(.4f, .72f, .79f, .18f), false);
        _coil = PartArt.Mesh(Visual, CoilMesh(), new("#ccd9df"), new(0, -.8f, 0));
        _pulley = new Node3D { Position = new(-.62f, -.55f, .5f) }; Visual.AddChild(_pulley);
        var wheel = PartArt.Cylinder(_pulley, .26f, .1f, new("#fff8e9"));
        wheel.RotationDegrees = new(90, 0, 0);
        PartArt.Box(_pulley, new(.42f, .05f, .03f), new("#f7cb52"), new(0, 0, .065f));
        _latch = new Node3D { Position = new(.48f, .25f, .5f) }; Visual.AddChild(_latch);
        PartArt.Box(_latch, new(.22f, .1f, .1f), new("#f7cb52"));
        PartArt.Sphere(Visual, .07f, new("#f7cb52"), new(.62f, -.55f, .55f));
        _head = new GuidedPlunger { Mechanism = this, Definition = new PartDefinition { Id = CatalogId }, TopLevel = true };
        _head.Configure(new() { Id = InternalBodyId(InternalBodyRole.Plunger), Kind = CatalogId });
        AddChild(_head);
        _internalBodies.Add(_head);
        _head.SetCoordinate(RestHeadY);
        _previousPose = Transform;
        UpdateArt();
    }
    private static ImmediateMesh CoilMesh()
    {
        const int segments = 96, sides = 8;
        var mesh = new ImmediateMesh(); mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        void Vertex(int step, int side)
        {
            var t = (float)step / segments;
            var angle = t * Mathf.Tau * 4;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var tangent = new Vector3(-Mathf.Sin(angle) * .22f * Mathf.Tau * 4, 1.38f,
                Mathf.Cos(angle) * .22f * Mathf.Tau * 4).Normalized();
            var around = (float)side / sides * Mathf.Tau;
            var normal = radial * Mathf.Cos(around) + tangent.Cross(radial) * Mathf.Sin(around);
            mesh.SurfaceSetNormal(normal);
            mesh.SurfaceAddVertex(radial * .22f + Vector3.Up * (t * 1.38f) + normal * .025f);
        }
        for (var step = 0; step < segments; step++)
        for (var side = 0; side < sides; side++)
        {
            Vertex(step, side); Vertex(step + 1, side); Vertex(step + 1, side + 1);
            Vertex(step, side); Vertex(step + 1, side + 1); Vertex(step, side + 1);
        }
        mesh.SurfaceEnd(); return mesh;
    }

    private partial class GuidedPlunger : MachinePart
    {
        public WoundSpringPart Mechanism { get; init; } = null!;
        public bool AtStop { get; set; } = true;
        public Vector3 Axis => Mechanism.Basis.Y.Normalized();
        public float Coordinate => (Position - Mechanism.Position).Dot(Axis);
        public override MachinePart PhysicsOwner => Mechanism;
        public override bool FreeMotion => Mechanism._spring.State == SpringLatchState.Releasing && !AtStop;
        public override Vector3 InverseMassResponse(Vector3 direction)
        {
            if (!FreeMotion) return Vector3.Zero;
            var axial = Axis.Dot(direction);
            // At rest the ratchet absorbs inward impulse: it is an anchored
            // contact, not a mass that can move backward and then be clamped.
            if (axial < 0 && Velocity.Dot(Axis) <= 0) return Vector3.Zero;
            return Axis * (axial / Mass);
        }
        public void SetCoordinate(float value) => Position = Mechanism.Position + Axis * value;
        public override void ConstrainVelocity() => Velocity = FreeMotion ? Axis * Mathf.Max(0, Velocity.Dot(Axis)) : Vector3.Zero;
        public override float TimeToMotionLimit() => FreeMotion && Velocity.Dot(Axis) > 0 ?
            Mathf.Max(0, RestHeadY - Coordinate) / Velocity.Dot(Axis) : float.PositiveInfinity;
        public override void ReachMotionLimit()
        {
            SetCoordinate(RestHeadY); Velocity = Vector3.Zero; AtStop = true;
        }
        public override void QuantizePhysics()
        {
            SetCoordinate(Coordinate);
            ConstrainVelocity();
        }
        protected override void Build()
        {
            Dynamic = true; Radius = PlungerRadius; Mass = PlungerMass; Bounce = 0; Drag = 0;
            PartArt.Sphere(Visual, Radius, new("#fff8e9"));
        }
    }
}
