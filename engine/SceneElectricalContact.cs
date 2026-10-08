using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum ElectricalContactKind { OwnerActive, BooleanState, CounterReached, LatchOn, TimerCounting, ContactLoaded, ServoEndpoint }

/// <summary>Immutable condition declaration. No executable payload or catalogue dispatch.</summary>
public readonly record struct ElectricalContactSignal
{
    public ElectricalContactKind Kind { get; }
    public SimulationState<bool>? Boolean { get; private init; }
    public SceneCounterKey? Counter { get; private init; }
    public SceneLatchKey? Latch { get; private init; }
    public SceneTimerKey? Timer { get; private init; }
    public SceneBodyKey? Body { get; private init; }
    public SceneJointKey? Joint { get; private init; }
    public PhysicsServoEndpoint Endpoint { get; private init; }
    public double Tolerance { get; private init; }
    private ElectricalContactSignal(ElectricalContactKind kind)=>Kind=kind;
    public static ElectricalContactSignal OwnerActive=>new(ElectricalContactKind.OwnerActive);
    public static ElectricalContactSignal BooleanState(SimulationState<bool> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new(ElectricalContactKind.BooleanState){Boolean=state};
    }
    public static ElectricalContactSignal CounterReached(SceneCounterKey key)
    {
        if(key.Owner is null||key.Slot is null)throw new ArgumentException("Counter contact requires a complete key.");
        return new(ElectricalContactKind.CounterReached){Counter=key};
    }
    public static ElectricalContactSignal LatchOn(SceneLatchKey key)
    {
        if(key.Owner is null||key.Slot is null)throw new ArgumentException("Latch contact requires a complete key.");
        return new(ElectricalContactKind.LatchOn){Latch=key};
    }
    public static ElectricalContactSignal TimerCounting(SceneTimerKey key)
    {
        if(key.Owner is null||key.Slot is null)throw new ArgumentException("Timer contact requires a complete key.");
        return new(ElectricalContactKind.TimerCounting){Timer=key};
    }
    public static ElectricalContactSignal ContactLoaded(SceneBodyKey key)
    {
        if(key.Owner is null||key.Slot is null)throw new ArgumentException("Load contact requires an owned body.");
        return new(ElectricalContactKind.ContactLoaded){Body=key};
    }
    public static ElectricalContactSignal ServoAtEndpoint(SceneJointKey key,PhysicsServoEndpoint endpoint,double tolerance)
    {
        if(key.Owner is null||key.Slot is null||!Enum.IsDefined(endpoint)||
            !double.IsFinite(tolerance)||tolerance<0)
            throw new ArgumentException("Servo contact requires a joint, supported endpoint and finite nonnegative tolerance.");
        return new(ElectricalContactKind.ServoEndpoint){Joint=key,Endpoint=endpoint,Tolerance=tolerance};
    }
    internal void ValidateOwner(MachinePart owner)
    {
        var valid=Kind switch
        {
            ElectricalContactKind.OwnerActive=>true,
            ElectricalContactKind.BooleanState=>Boolean is { } cell&&owner.RuntimeState.Any(state=>ReferenceEquals(state,cell)),
            ElectricalContactKind.CounterReached=>Counter is { } counter&&counter.Owner==owner&&owner.SimulationCounters.Any(d=>d.Key==counter),
            ElectricalContactKind.LatchOn=>Latch is { } latch&&latch.Owner==owner&&owner.SimulationLatches.Any(d=>d.Key==latch),
            ElectricalContactKind.TimerCounting=>Timer is { } timer&&timer.Owner==owner&&owner.SimulationTimers.Any(d=>d.Key==timer),
            ElectricalContactKind.ContactLoaded=>Body is { } body&&body.Owner==owner&&owner.PhysicsContactLoadSensors.Any(d=>d.Frame==body),
            ElectricalContactKind.ServoEndpoint=>Joint is { } joint&&joint.Owner==owner&&owner.PhysicsServos.Any(d=>d.Joint==joint),
            _=>false
        };
        if(!valid)throw new ArgumentException("Electrical contact must bind a declared capability or checkpoint cell of its owner.");
    }
}

/// <summary>Run construction data, consumed only while resolving contact IDs.</summary>
internal sealed record ElectricalRuntime(
    ScenePhysicsAssembly Assembly,PhysicsWorld Physics,
    SimulationCounters Counters,IReadOnlyDictionary<SceneCounterKey,SimulationCounterId> CounterIds,
    SimulationLatches Latches,IReadOnlyDictionary<SceneLatchKey,SimulationLatchId> LatchIds,
    SimulationTimers Timers,IReadOnlyDictionary<SceneTimerKey,SimulationTimerId> TimerIds,
    IReadOnlyDictionary<SceneBinaryInputKey,SimulationState<Bridge.BinaryInputState>> BinaryInputs);

/// <summary>
/// One captured condition. Activity/cell conditions are usable without controller registration.
/// Controller/physics conditions reject sampling until explicitly bound; no construction substitute.
/// </summary>
internal sealed class ElectricalContactBinding
{
    private readonly MachinePart _owner;
    private readonly ElectricalContactSignal _signal;
    private readonly SimulationCounters? _counters;
    private readonly SimulationLatches? _latches;
    private readonly SimulationTimers? _timers;
    private readonly PhysicsWorld? _physics;
    private readonly SimulationCounterId _counter;
    private readonly SimulationLatchId _latch;
    private readonly SimulationTimerId _timer;
    private readonly PhysicsBodyId _body;
    private readonly PhysicsJointId _joint;
    public ElectricalContactBinding(MachinePart owner,ElectricalContactSignal signal)
    {
        ArgumentNullException.ThrowIfNull(owner);
        signal.ValidateOwner(owner);_owner=owner;_signal=signal;
    }
    private ElectricalContactBinding(ElectricalContactBinding declaration,ElectricalRuntime runtime)
    {
        _owner=declaration._owner;_signal=declaration._signal;
        switch(_signal.Kind)
        {
            case ElectricalContactKind.OwnerActive:
            case ElectricalContactKind.BooleanState:break;
            case ElectricalContactKind.CounterReached:
                if(!runtime.CounterIds.TryGetValue(_signal.Counter!.Value,out _counter))
                    throw new ArgumentException("Electrical counter binding is absent.");
                _counters=runtime.Counters;_=_counters.Read(_counter);break;
            case ElectricalContactKind.LatchOn:
                if(!runtime.LatchIds.TryGetValue(_signal.Latch!.Value,out _latch))
                    throw new ArgumentException("Electrical latch binding is absent.");
                _latches=runtime.Latches;_=_latches.Read(_latch);break;
            case ElectricalContactKind.TimerCounting:
                if(!runtime.TimerIds.TryGetValue(_signal.Timer!.Value,out _timer))
                    throw new ArgumentException("Electrical timer binding is absent.");
                _timers=runtime.Timers;_=_timers.Read(_timer);break;
            case ElectricalContactKind.ContactLoaded:
                _physics=runtime.Physics;_body=runtime.Assembly.Body(_signal.Body!.Value).Id;
                _=_physics.ContactLoad(_body);break;
            case ElectricalContactKind.ServoEndpoint:
                _physics=runtime.Physics;_joint=runtime.Assembly.JointId(_signal.Joint!.Value);
                _=_physics.Servo(_joint);break;
            default:throw new ArgumentException("Unsupported electrical contact condition.");
        }
    }
    public ElectricalContactBinding Bind(ElectricalRuntime runtime)=>new(this,runtime);
    public bool Read()=>_signal.Kind switch
    {
        ElectricalContactKind.OwnerActive=>_owner.Active,
        ElectricalContactKind.BooleanState=>_signal.Boolean!.Value,
        ElectricalContactKind.CounterReached=>Require(_counters).Read(_counter).Phase==SimulationCounterPhase.Reached,
        ElectricalContactKind.LatchOn=>Require(_latches).Read(_latch).Phase==SimulationLatchPhase.On,
        ElectricalContactKind.TimerCounting=>Require(_timers).Read(_timer).Phase==SimulationTimerPhase.Counting,
        ElectricalContactKind.ContactLoaded=>Require(_physics).ContactLoad(_body).Phase==PhysicsContactLoadPhase.Loaded,
        ElectricalContactKind.ServoEndpoint=>Require(_physics).Servo(_joint).AtEndpoint(_signal.Endpoint,_signal.Tolerance),
        _=>throw new InvalidOperationException("Unsupported electrical contact condition.")
    };
    private static T Require<T>(T? value) where T:class=>value??
        throw new InvalidOperationException("Electrical contact requires an explicitly bound runtime.");
}
