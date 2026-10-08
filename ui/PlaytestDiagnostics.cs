using Godot;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

// Read-only instrumentation, compiled out of normal builds. No setters, commands or auto-solver.
public partial class Workshop
{
    private string _lastPlaytestUi = "";
    private bool _lastPlaytestRunning;
    private static float[] ScreenPoint(Vector2 point) => [point.X, point.Y];
    private static float[] Point(Vector3 point) => [point.X, point.Y, point.Z];

    [Conditional("PLAYTEST")]
    private void TracePlaytestStart()
    {
        GD.Print("CCRUN " + JsonSerializer.Serialize(new PlaytestRun
        {
            Level = _currentLevel + 1, Precision = World.Precision,
            Connections = World.Connections.ToList(),
            Parts = World.Parts.Select(p => new PlaytestPart
            {
                Id = p.Uid, Kind = p.Definition.Id, Locked = p.Locked, Dynamic = p.Dynamic,
                Properties = new(p.Properties), Position = Point(p.Position), Orientation = SceneOrientation.Capture(p.Basis)
            }).ToList()
        }, PlaytestJson.Default.PlaytestRun));
        TracePlaytestFrame();
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestReset()
    {
        GD.Print("CCRESET " + JsonSerializer.Serialize(new PlaytestRun
        {
            Level = _currentLevel + 1, Precision = World.Precision,
            Connections = World.Connections.ToList(),
            Parts = World.Parts.Select(p => new PlaytestPart
            {
                Id = p.Uid, Kind = p.Definition.Id, Locked = p.Locked, Dynamic = p.Dynamic,
                Properties = new(p.Properties), Position = Point(p.Position), Orientation = SceneOrientation.Capture(p.Basis)
            }).ToList()
        }, PlaytestJson.Default.PlaytestRun));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestFrame()
    {
        // Dense early placement trace, followed by 10 Hz behavior evidence.
        // All observations are read-only; no game commands are exposed.
        if (World.Ticks % (World.Ticks <= 120 ? 4 : 12) != 0) return;
        GD.Print("CCFRAME " + JsonSerializer.Serialize(new PlaytestFrame
        {
            Tick = World.Ticks,
#if PLAYTEST
            Performance=PlaytestPerformanceBatch.Capture(World.DrainPerformance()),
#endif
            Latches = World.Parts.SelectMany(p=>p.SimulationLatches)
                .Select(d=>PlaytestLatch.Capture(World,d)).ToList(),
            Counters = World.Parts.SelectMany(p=>p.SimulationCounters)
                .Select(d=>PlaytestCounter.Capture(World,d)).ToList(),
            Oscillators = World.Parts.SelectMany(p=>p.SimulationOscillators)
                .Select(d=>PlaytestOscillator.Capture(World,d)).ToList(),
            Timers = World.Parts.SelectMany(p=>p.SimulationTimers)
                .Select(d=>PlaytestTimer.Capture(World,d)).ToList(),
            Electrical = World.Parts.SelectMany(p=>p.ConnectionPorts
                .Where(port=>port.Domain==ConnectionDomain.Electrical&&port.Direction==PortDirection.Input)
                .Select(port=>new PlaytestElectrical
                {
                    Part=PlaytestPartIdentity.FromBoundary(p.Uid),Port=port.Id,
                    Powered=p.HasElectricalPower(port.Id)
                })).ToList(),
            Bodies = World.Bodies.Select(p => PlaytestBody.Capture(World,p)).ToList(),
            Mechanical = World.Parts.SelectMany(p => p.ConnectionPorts
                .Where(port => port.Domain == ConnectionDomain.Mechanical)
                .Select(port => new PlaytestMechanical
                {
                    Id = p.Uid, Port = port.Id, Speed = MechanicalNetwork.Speed(World,p,port.Id),
                    Joint = MechanicalNetwork.Shaft(World,p,port.Id).Id,
                    KineticEnergy = MechanicalNetwork.Shaft(World,p,port.Id).A.KineticEnergy
                })).ToList(),
            Cannons = World.Parts.OfType<CannonPart>().Select(p => new PlaytestCannon
            {
                Id = p.Uid, Phase = p.Phase, LastShot = p.LastShot, ShotCount = p.ShotCount,
                StoredEnergy = p.StoredEnergy, ReleasedEnergy = p.ReleasedEnergy,
                PayloadId = p.LastPayload?.Uid, RecoilOffset = p.RecoilOffset
            }).ToList(),
            FrameJoints = World.Physics.Joints.ToArray().OfType<PhysicsFrameJoint>()
                .Select(joint=>PlaytestFrameJoint.Capture(World.Physics,joint)).ToList(),
            WoundSprings = World.Parts.OfType<WoundSpringPart>().Select(p => new PlaytestWoundSpring
            {
                Id = p.Uid, Phase = p.Phase, LastTrigger = p.LastTrigger, ReleaseCount = p.ReleaseCount,
                Compression = p.Compression, StoredEnergy = p.StoredEnergy,
                AcceptedWork = p.AcceptedWork, ReleasedWork = p.ReleasedWork
            }).ToList(),
            Parts = World.Parts.Where(p => !p.Locked && !p.Dynamic)
                .Select(p => PlaytestPart.CaptureRuntime(World,p)).ToList()
        }, PlaytestJson.Default.PlaytestFrame));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestResult(string outcome)
    {
        GD.Print("CCRESULT " + JsonSerializer.Serialize(new PlaytestResult
        { Level = _currentLevel + 1, Precision = World.Precision, Tick = World.Ticks, Outcome = outcome },
            PlaytestJson.Default.PlaytestResult));
    }

    [Conditional("PLAYTEST")]
    private void TracePlaytestUi()
    {
        // Emit the running controls once, so UI-only tests can press Reset before a result.
        // Subsequent moving-body geometry is unnecessary during simulation.
        if (_inRun && World.Running && _lastPlaytestRunning) return;
        _lastPlaytestRunning = _inRun;
        var ui = new PlaytestUi
        {
            Level = _currentLevel + 1, Running = _inRun, MenuOpen = _optionsPanel.Visible,
            Precision = World.Precision,
            DifficultyLeft = ScreenPoint(_precision.GetGlobalTransformWithCanvas() * new Vector2(2, _precision.Size.Y * .5f)),
            DifficultyRight = ScreenPoint(_precision.GetGlobalTransformWithCanvas() * new Vector2(_precision.Size.X - 2, _precision.Size.Y * .5f))
        };
        foreach (var button in FindChildren("*", "Button", true, false).OfType<WorkshopButton>())
        {
            if (!button.IsVisibleInTree()) continue;
            var point = button.GetGlobalTransformWithCanvas() * (button.Size * .5f);
            var clipped = false;
            for (Node? parent = button.GetParent(); parent != null; parent = parent.GetParent())
                if (parent is ScrollContainer scroll &&
                    !new Rect2(scroll.GetGlobalTransformWithCanvas().Origin,
                        scroll.Size * _canvas.Transform.Scale).HasPoint(point)) clipped = true;
            ui.Buttons.Add(new()
            {
                Action = button.GetMeta("action_label").AsString(),
                Kind = button.HasMeta("part_kind") ? button.GetMeta("part_kind").AsString() : "",
                Screen = ScreenPoint(point), Enabled = !button.Disabled, Clipped = clipped
            });
        }
        foreach (var part in World.Parts.Where(p => p.Visible))
            ui.Parts.Add(new() { Id = part.Uid, Kind = part.Definition.Id,
                Screen = ScreenPoint(_camera.UnprojectPosition(part.GlobalPosition)) });
        if (_selected != null) ui.Selected = _selected.Uid;
        if (_selected != null && _rotationGizmo.Visible)
        {
            ui.Mode = _rotationGizmo.ResizeMode ? "resize" : _rotationGizmo.MoveMode ? "move" : "rotate";
            if (_selected is IResizablePart resizable) ui.Dimensions = Point(resizable.Dimensions);
            ui.Center = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition));
            var axes = new[] { Vector3.Right, Vector3.Up, Vector3.Back };
            for (var index = 0; index < axes.Length; index++)
            {
                if (!_rotationGizmo.AxisEnabled(index)) continue;
                var axis = axes[index];
                var direction = _rotationGizmo.ResizeMode ? _selected.GlobalBasis * axis : axis;
                var handle = _rotationGizmo.HandlePosition(index);
                // Projected visible handle geometry, analogous to a DOM element's bounding box.
                ui.Handles.Add(new()
                {
                    Axis = index,
                    Screen = ScreenPoint(_camera.UnprojectPosition(handle)),
                    Unit = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition + direction) -
                        _camera.UnprojectPosition(_selected.GlobalPosition)),
                    Quarter = ScreenPoint(_camera.UnprojectPosition(_selected.GlobalPosition +
                        axis.Cross(handle - _selected.GlobalPosition)))
                });
            }
        }
        else if (_selected != null) ui.Selected = _selected.Uid;
        var json = JsonSerializer.Serialize(ui, PlaytestJson.Default.PlaytestUi);
        if (json == _lastPlaytestUi) return;
        _lastPlaytestUi = json;
        GD.Print("CCUI " + json);
    }
}

public sealed class PlaytestButton
{
    public string Action { get; set; } = "";
    public string Kind { get; set; } = "";
    public float[] Screen { get; set; } = [];
    public bool Enabled { get; set; }
    public bool Clipped { get; set; }
}
public sealed class PlaytestHandle
{
    public int Axis { get; set; }
    public float[] Screen { get; set; } = [];
    public float[] Unit { get; set; } = [];
    public float[] Quarter { get; set; } = [];
}
public sealed class PlaytestUiPart
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public float[] Screen { get; set; } = [];
}
public sealed class PlaytestUi
{
    public float Precision { get; set; }
    public float[] DifficultyLeft { get; set; } = [];
    public float[] DifficultyRight { get; set; } = [];
    public int Level { get; set; }
    public bool Running { get; set; }
    public bool MenuOpen { get; set; }
    public string Selected { get; set; } = "";
    public string Mode { get; set; } = "";
    public float[] Dimensions { get; set; } = [];
    public float[] Center { get; set; } = [];
    public List<PlaytestButton> Buttons { get; set; } = new();
    public List<PlaytestUiPart> Parts { get; set; } = new();
    public List<PlaytestHandle> Handles { get; set; } = new();
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestPart
{
    /// <summary>External diagnostic DTO from authoritative state, independent of rendered poses.</summary>
    public static PlaytestPart CaptureRuntime(MachineWorld world,MachinePart part)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(part);
        if(world.Phase!=MachineWorldPhase.Idle||!world.HasPhysicsState)
            throw new InvalidOperationException("Runtime part diagnostics require a committed run.");
        var pose=WorldGeometry.CaptureSpatialState(world,new(part,MachinePart.RootBody)).Pose.ToScene();
        return new()
        {
            Id=part.Uid,Kind=part.Definition.Id,Locked=part.Locked,Dynamic=part.Dynamic,
            Properties=new(part.Properties),Position=[pose.Origin.X,pose.Origin.Y,pose.Origin.Z],
            Orientation=SceneOrientation.Capture(pose.Basis)
        };
    }
    public Dictionary<string, float> Properties { get; set; } = new();
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Locked { get; set; }
    public bool Dynamic { get; set; }
    public float[] Position { get; set; } = [];
    [JsonRequired]
    public PartOrientation Orientation
    {
        get;
        set { ArgumentNullException.ThrowIfNull(value); field=value; }
    } = PartOrientation.Identity;
}
public sealed class PlaytestRun
{
    public List<ConnectionSpec> Connections { get; set; } = new();
    public int Level { get; set; }
    public float Precision { get; set; }
    public List<PlaytestPart> Parts { get; set; } = new();
}
public sealed class PlaytestBody
{
    /// <summary>Read owned runtime state without requiring a render/presentation update.
    /// Position remains the part origin, not its possibly offset mass centre.
    /// Visible is a presentation observation, not collision participation.</summary>
    public static PlaytestBody Capture(MachineWorld world,MachinePart part)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(part);
        if(world.Physics.Phase!=PhysicsWorldPhase.Idle)
            throw new InvalidOperationException("Body diagnostics require a committed physics state.");
        var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
        var origin=body.Center-body.Pose.Rotation.Apply(SceneGeometryAdapter.CaptureVector(part.LocalCenterOfMass));
        var velocity=body.LinearVelocity;
        return new()
        {
            Id=part.Uid,Visible=part.Visible,
            Position=[(float)origin.X,(float)origin.Y,(float)origin.Z],
            Velocity=[(float)velocity.X,(float)velocity.Y,(float)velocity.Z]
        };
    }

    public string Id { get; set; } = "";
    public bool Visible { get; set; }
    public float[] Position { get; set; } = [];
    public float[] Velocity { get; set; } = [];
}
public sealed class PlaytestCannonPhaseConverter : ExactPlaytestEnumConverter<CannonPhase>;
public sealed class PlaytestCannonShotConverter : ExactPlaytestEnumConverter<CannonShotResult>;
public sealed class PlaytestCannon
{
    public string Id { get; set; } = "";
    [JsonConverter(typeof(PlaytestCannonPhaseConverter))]
    public CannonPhase Phase { get; set; }
    [JsonConverter(typeof(PlaytestCannonShotConverter))]
    public CannonShotResult LastShot { get; set; }
    public int ShotCount { get; set; }
    public double StoredEnergy { get; set; }
    public double ReleasedEnergy { get; set; }
    public string? PayloadId { get; set; }
    public float RecoilOffset { get; set; }
}
public sealed class PlaytestWoundSpringPhaseConverter : ExactPlaytestEnumConverter<WoundSpringPhase>;
public sealed class PlaytestSpringTriggerConverter : ExactPlaytestEnumConverter<SpringTriggerResult>;
public sealed class PlaytestWoundSpring
{
    public string Id { get; set; } = "";
    [JsonConverter(typeof(PlaytestWoundSpringPhaseConverter))]
    public WoundSpringPhase Phase { get; set; }
    [JsonConverter(typeof(PlaytestSpringTriggerConverter))]
    public SpringTriggerResult? LastTrigger { get; set; }
    public int ReleaseCount { get; set; }
    public float Compression { get; set; }
    public double StoredEnergy { get; set; }
    public double AcceptedWork { get; set; }
    public double ReleasedWork { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestMechanical
{
    public string Id { get; set; } = "";
    public SocketId Port { get; set; }
    public float Speed { get; set; }
    [JsonConverter(typeof(PlaytestJointIdConverter))]
    public required PhysicsJointId Joint { get; set; }
    public required double KineticEnergy { get; set; }
}
public sealed class PlaytestFrameJointKindConverter : ExactPlaytestEnumConverter<FrameJointKind>;
public sealed class PlaytestJointIdConverter : JsonConverter<PhysicsJointId>
{
    public override PhysicsJointId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Joint identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,PhysicsJointId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
public sealed class PlaytestBodyIdConverter : JsonConverter<PhysicsBodyId>
{
    public override PhysicsBodyId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Body identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,PhysicsBodyId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
/// <summary>Read-only external snapshot of a live shared frame joint, not a
/// second part-specific hinge model. Ball sockets have no axial coordinate.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestFrameJoint
{
    [JsonConverter(typeof(PlaytestJointIdConverter))]
    public PhysicsJointId Id { get; set; }
    [JsonConverter(typeof(PlaytestBodyIdConverter))]
    public PhysicsBodyId BodyA { get; set; }
    [JsonConverter(typeof(PlaytestBodyIdConverter))]
    public PhysicsBodyId BodyB { get; set; }
    [JsonConverter(typeof(PlaytestFrameJointKindConverter))]
    public FrameJointKind Kind { get; set; }
    public double? Coordinate { get; set; }
    public double? Speed { get; set; }
    public double? Lower { get; set; }
    public double? Upper { get; set; }
    public double Energy { get; set; }
    public double? Winding { get; set; }
    public double? AngularDistance { get; set; }
    public double? AngularDistanceError { get; set; }
    public static PlaytestFrameJoint Capture(PhysicsWorld world,PhysicsFrameJoint joint)=>new()
    {
        Id=joint.Id,BodyA=joint.A.Id,BodyB=joint.B.Id,Kind=joint.Kind,
        Coordinate=joint.Kind==FrameJointKind.BallSocket?null:joint.Travel.Error,
        Speed=joint.Kind==FrameJointKind.BallSocket?null:joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed,
        Lower=joint.TravelRange?.Lower,Upper=joint.TravelRange?.Upper,
        Winding=joint.Kind==FrameJointKind.Hinge?world.AngularTravel(joint.Id).Winding:null,
        AngularDistance=joint.Kind==FrameJointKind.Hinge?world.AngularTravel(joint.Id).Distance:null,
        AngularDistanceError=joint.Kind==FrameJointKind.Hinge?world.AngularTravel(joint.Id).DistanceError:null,
        Energy=joint.A.KineticEnergy+joint.B.KineticEnergy
    };
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestFrame
{
    public PlaytestPerformanceBatch? Performance { get; set; }
    public List<PlaytestLatch> Latches { get; set; } = new();
    public List<PlaytestCounter> Counters { get; set; } = new();
    public List<PlaytestOscillator> Oscillators { get; set; } = new();
    public List<PlaytestTimer> Timers { get; set; } = new();
    public List<PlaytestElectrical> Electrical { get; set; } = new();
    public List<PlaytestFrameJoint> FrameJoints { get; set; } = new();
    public List<PlaytestMechanical> Mechanical { get; set; } = new();
    public List<PlaytestBody> Bodies { get; set; } = new();
    public List<PlaytestCannon> Cannons { get; set; } = new();
    public List<PlaytestWoundSpring> WoundSprings { get; set; } = new();
    public int Tick { get; set; }
    public List<PlaytestPart> Parts { get; set; } = new();
}
public sealed class PlaytestResult
{
    public int Level { get; set; }
    public int Tick { get; set; }
    public float Precision { get; set; }
    public string Outcome { get; set; } = "";
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(PlaytestPerformanceSample))]
[JsonSerializable(typeof(PlaytestPerformanceCounter))]
[JsonSerializable(typeof(PlaytestUi))]
[JsonSerializable(typeof(PlaytestRun))]
[JsonSerializable(typeof(PlaytestConstruction))]
[JsonSerializable(typeof(PlaytestFrame))]
[JsonSerializable(typeof(PlaytestResult))]
public partial class PlaytestJson : JsonSerializerContext { }

[JsonConverter(typeof(PlaytestPartIdentityConverter))]
public readonly record struct PlaytestPartIdentity
{
    public string Value { get; }
    private PlaytestPartIdentity(string value)=>Value=value;
    public static PlaytestPartIdentity FromBoundary(string value)=>
        !string.IsNullOrWhiteSpace(value)?new(value):throw new ArgumentException("Part identity cannot be empty.");
}
public sealed class PlaytestPartIdentityConverter : JsonConverter<PlaytestPartIdentity>
{
    public override PlaytestPartIdentity Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
    {
        if(reader.TokenType!=JsonTokenType.String)throw new JsonException("Part identity must be text.");
        var value=reader.GetString();
        if(string.IsNullOrWhiteSpace(value))throw new JsonException("Part identity cannot be empty.");
        return PlaytestPartIdentity.FromBoundary(value);
    }
    public override void Write(Utf8JsonWriter writer,PlaytestPartIdentity value,JsonSerializerOptions options)
    {
        if(string.IsNullOrWhiteSpace(value.Value))throw new JsonException("Part identity cannot be empty.");
        writer.WriteStringValue(value.Value);
    }
}
public sealed class PlaytestTimerPhaseConverter : ExactPlaytestEnumConverter<SimulationTimerPhase>;
public sealed class PlaytestTimerCompletionConverter : ExactPlaytestEnumConverter<TimerCompletionPolicy>;
public sealed class PlaytestTimerBoundaryConverter : ExactPlaytestEnumConverter<TimerBoundary>;
public sealed class PlaytestTimerIdConverter : JsonConverter<SimulationTimerId>
{
    public override SimulationTimerId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Timer identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,SimulationTimerId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestTimer
{
    public required PlaytestPartIdentity Part { get; set; }
    [JsonConverter(typeof(PlaytestTimerIdConverter))]
    public required SimulationTimerId Id { get; set; }
    [JsonConverter(typeof(PlaytestTimerPhaseConverter))]
    public required SimulationTimerPhase Phase { get; set; }
    [JsonConverter(typeof(PlaytestTimerCompletionConverter))]
    public required TimerCompletionPolicy Completion { get; set; }
    [JsonConverter(typeof(PlaytestTimerBoundaryConverter))]
    public required TimerBoundary Boundary { get; set; }
    public required int StartedTick { get; set; }
    public required int DueTick { get; set; }
    public required double Progress { get; set; }
    public static PlaytestTimer Capture(MachineWorld world,SceneTimerDeclaration declaration)
    {
        if(world.Phase!=MachineWorldPhase.Idle||world.Timers.TransactionPhase!=SimulationTransactionPhase.Idle)
            throw new InvalidOperationException("Timer diagnostics require committed timer state.");
        var state=world.ReadTimer(declaration.Key);
        return new()
        {
            Part=PlaytestPartIdentity.FromBoundary(declaration.Key.Owner.Uid),Id=state.Id,Phase=state.Phase,
            Completion=declaration.Completion,Boundary=declaration.Boundary,
            StartedTick=state.StartedTick,DueTick=state.DueTick,Progress=world.TimerProgress(declaration.Key)
        };
    }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestElectrical
{
    public required PlaytestPartIdentity Part { get; set; }
    public required SocketId Port { get; set; }
    public required bool Powered { get; set; }
}

public sealed class PlaytestOscillatorPhaseConverter : ExactPlaytestEnumConverter<SimulationOscillatorPhase>;
public sealed class PlaytestOscillatorIdConverter : JsonConverter<SimulationOscillatorId>
{
    public override SimulationOscillatorId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Oscillator identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,SimulationOscillatorId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestOscillator
{
    public required PlaytestPartIdentity Part { get; set; }
    [JsonConverter(typeof(PlaytestOscillatorIdConverter))]
    public required SimulationOscillatorId Id { get; set; }
    [JsonConverter(typeof(PlaytestOscillatorPhaseConverter))]
    public required SimulationOscillatorPhase Phase { get; set; }
    public required int DueTick { get; set; }
    public required int PulseCount { get; set; }
    public required int LastPulseTick { get; set; }
    public required double Progress { get; set; }
    public static PlaytestOscillator Capture(MachineWorld world,SceneOscillatorDeclaration declaration)
    {
        if(world.Phase!=MachineWorldPhase.Idle||world.Oscillators.TransactionPhase!=SimulationTransactionPhase.Idle)
            throw new InvalidOperationException("Oscillator diagnostics require committed state.");
        var state=world.ReadOscillator(declaration.Key);
        return new()
        {
            Part=PlaytestPartIdentity.FromBoundary(declaration.Key.Owner.Uid),Id=state.Id,Phase=state.Phase,
            DueTick=state.DueTick,PulseCount=state.PulseCount,LastPulseTick=state.LastPulseTick,
            Progress=world.OscillatorProgress(declaration.Key)
        };
    }
}

public sealed class PlaytestCounterPhaseConverter : ExactPlaytestEnumConverter<SimulationCounterPhase>;
public sealed class PlaytestCounterIdConverter : JsonConverter<SimulationCounterId>
{
    public override SimulationCounterId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Counter identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,SimulationCounterId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestCounter
{
    public required PlaytestPartIdentity Part { get; set; }
    [JsonConverter(typeof(PlaytestCounterIdConverter))]
    public required SimulationCounterId Id { get; set; }
    [JsonConverter(typeof(PlaytestCounterPhaseConverter))]
    public required SimulationCounterPhase Phase { get; set; }
    public required int Count { get; set; }
    public required int Target { get; set; }
    public static PlaytestCounter Capture(MachineWorld world,SceneCounterDeclaration declaration)
    {
        if(world.Phase!=MachineWorldPhase.Idle||world.Counters.TransactionPhase!=SimulationTransactionPhase.Idle)
            throw new InvalidOperationException("Counter diagnostics require committed state.");
        var state=world.ReadCounter(declaration.Key);
        return new()
        {
            Part=PlaytestPartIdentity.FromBoundary(declaration.Key.Owner.Uid),Id=state.Id,
            Phase=state.Phase,Count=state.Count,Target=state.Target
        };
    }
}

public sealed class PlaytestLatchPhaseConverter : ExactPlaytestEnumConverter<SimulationLatchPhase>;
public sealed class PlaytestLatchRequestsConverter : JsonConverter<SimulationLatchRequests>
{
    public override SimulationLatchRequests Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType!=JsonTokenType.String?throw new JsonException("Latch requests require a canonical string."):reader.GetString() switch
        {
            "none"=>SimulationLatchRequests.None,
            "set"=>SimulationLatchRequests.Set,
            "reset"=>SimulationLatchRequests.Reset,
            "set_and_reset"=>SimulationLatchRequests.Set|SimulationLatchRequests.Reset,
            _=>throw new JsonException("Unsupported latch requests.")
        };
    public override void Write(Utf8JsonWriter writer,SimulationLatchRequests value,JsonSerializerOptions options)=>
        writer.WriteStringValue(value switch
        {
            SimulationLatchRequests.None=>"none",
            SimulationLatchRequests.Set=>"set",
            SimulationLatchRequests.Reset=>"reset",
            SimulationLatchRequests.Set|SimulationLatchRequests.Reset=>"set_and_reset",
            _=>throw new JsonException("Unsupported latch requests.")
        });
}
public sealed class PlaytestLatchIdConverter : JsonConverter<SimulationLatchId>
{
    public override SimulationLatchId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
        reader.TokenType==JsonTokenType.Number&&reader.TryGetInt32(out var value)&&value>=0
            ?new(value):throw new JsonException("Latch identity must be a nonnegative integer.");
    public override void Write(Utf8JsonWriter writer,SimulationLatchId value,JsonSerializerOptions options)=>
        writer.WriteNumberValue(value.Index);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlaytestLatch
{
    public required PlaytestPartIdentity Part { get; set; }
    [JsonConverter(typeof(PlaytestLatchIdConverter))]
    public required SimulationLatchId Id { get; set; }
    [JsonConverter(typeof(PlaytestLatchPhaseConverter))]
    public required SimulationLatchPhase Phase { get; set; }
    public required long BoundaryTick { get; set; }
    [JsonConverter(typeof(PlaytestLatchRequestsConverter))]
    public required SimulationLatchRequests CurrentRequests { get; set; }
    [JsonConverter(typeof(PlaytestLatchRequestsConverter))]
    public required SimulationLatchRequests NextRequests { get; set; }
    public static PlaytestLatch Capture(MachineWorld world,SceneLatchDeclaration declaration)
    {
        if(world.Phase!=MachineWorldPhase.Idle||world.Latches.TransactionPhase!=SimulationTransactionPhase.Idle)
            throw new InvalidOperationException("Latch diagnostics require committed state.");
        var state=world.ReadLatch(declaration.Key);
        return new()
        {
            Part=PlaytestPartIdentity.FromBoundary(declaration.Key.Owner.Uid),Id=state.Id,Phase=state.Phase,
            BoundaryTick=world.Latches.Tick,CurrentRequests=state.CurrentRequests,NextRequests=state.NextRequests
        };
    }
}
