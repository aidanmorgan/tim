using System;
using CuriousContraptions.Physics;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public enum ScalarObservationStorage { Single, Double, VectorComponent }
public enum ScalarVectorComponent { X, Y, Z }

/// <summary>Owned numeric cell, with explicit storage type and no executable payload.</summary>
public readonly record struct ScalarObservationSource
{
    public ScalarObservationStorage Storage { get; }
    private readonly SimulationState<float>? _single;
    private readonly SimulationState<double>? _double;
    private readonly SimulationState<Godot.Vector3>? _vector;
    private readonly ScalarVectorComponent _component;
    public ScalarObservationSource(SimulationState<float> value)
    {
        ArgumentNullException.ThrowIfNull(value);Storage=ScalarObservationStorage.Single;_single=value;
    }
    public ScalarObservationSource(SimulationState<double> value)
    {
        ArgumentNullException.ThrowIfNull(value);Storage=ScalarObservationStorage.Double;_double=value;
    }
    public ScalarObservationSource(SimulationState<Godot.Vector3> value,ScalarVectorComponent component)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(!Enum.IsDefined(component))throw new ArgumentOutOfRangeException(nameof(component));
        Storage=ScalarObservationStorage.VectorComponent;_vector=value;_component=component;
    }
    internal SimulationTransactionParticipant Cell=>Storage switch
    {
        ScalarObservationStorage.Single=>_single??throw new ArgumentException("Missing scalar cell."),
        ScalarObservationStorage.Double=>_double??throw new ArgumentException("Missing scalar cell."),
        ScalarObservationStorage.VectorComponent=>_vector??throw new ArgumentException("Missing vector cell."),
        _=>throw new ArgumentException("Unsupported scalar storage.")
    };
    internal double Read()
    {
        _=Cell;
        var value=Storage switch
        {
            ScalarObservationStorage.Single=>_single!.Value,
            ScalarObservationStorage.Double=>_double!.Value,
            ScalarObservationStorage.VectorComponent=>_component switch
            {
                ScalarVectorComponent.X=>_vector!.Value.X,
                ScalarVectorComponent.Y=>_vector!.Value.Y,
                ScalarVectorComponent.Z=>_vector!.Value.Z,
                _=>throw new InvalidOperationException("Unsupported vector component.")
            },
            _=>throw new InvalidOperationException("Unsupported scalar storage.")
        };
        if(!double.IsFinite(value))throw new InvalidOperationException("Scalar observation must be finite.");
        return value;
    }
}
public readonly record struct SceneScalarObservation(ScalarObservationSlot Slot,ScalarUnit Unit,ScalarObservationSource Source);
/// <summary>A dimensionless observation of a declared, owned digital timer.</summary>
public readonly record struct SceneTimerObservation(ScalarObservationSlot Slot,SceneTimerKey Timer,SimulationTimerQuantity Quantity);
/// <summary>A dimensionless observation of a declared, owned digital oscillator.</summary>
public readonly record struct SceneOscillatorObservation(ScalarObservationSlot Slot,SceneOscillatorKey Oscillator,SimulationOscillatorQuantity Quantity);
public enum EnergyStoreQuantity { FillFraction, Energy, Capacity, AcceptedEnergy, ReleasedEnergy }
public readonly record struct SceneEnergyStoreObservation(ScalarObservationSlot Slot,SceneBodyKey Body,EnergyStoreQuantity Quantity);
public readonly record struct SceneScalarObservationKey(MachinePart Owner,ScalarObservationSlot Slot);

/// <summary>Run-scoped producer scratch. Publication copies it inside the tick transaction.</summary>
internal sealed class SceneScalarObservations
{
    private readonly ScalarObservationSource[] _sources;
    private readonly PhysicsWorld _physics;
    private readonly record struct EnergyBinding(int ReadIndex,PhysicsBodyId Body,EnergyStoreQuantity Quantity);
    private readonly EnergyBinding[] _energySources;
    private static double ReadEnergy(PhysicsEnergyStoreState state,EnergyStoreQuantity quantity)=>quantity switch
    {
        EnergyStoreQuantity.FillFraction=>state.Energy/state.Capacity,
        EnergyStoreQuantity.Energy=>state.Energy,
        EnergyStoreQuantity.Capacity=>state.Capacity,
        EnergyStoreQuantity.AcceptedEnergy=>state.AcceptedEnergy,
        EnergyStoreQuantity.ReleasedEnergy=>state.ReleasedEnergy,
        _=>throw new ArgumentOutOfRangeException(nameof(quantity))
    };
    private readonly ScalarRead[] _reads;
    private readonly record struct TimerBinding(int ReadIndex,SimulationTimerId Id,SimulationTimerQuantity Quantity);
    private readonly TimerBinding[] _timerSources;
    private readonly SimulationTimers _timers;
    private readonly record struct OscillatorBinding(int ReadIndex,SimulationOscillatorId Id,SimulationOscillatorQuantity Quantity);
    private readonly OscillatorBinding[] _oscillatorSources;
    private readonly SimulationOscillators _oscillators;
    public SceneScalarObservations(IReadOnlyList<MachinePart> parts,ScenePhysicsAssembly assembly,PhysicsWorld physics,
        SimulationTimers timers,IReadOnlyDictionary<SceneTimerKey,SimulationTimerId> timerIds,
        SimulationOscillators oscillators,IReadOnlyDictionary<SceneOscillatorKey,SimulationOscillatorId> oscillatorIds)
    {
        ArgumentNullException.ThrowIfNull(timers);ArgumentNullException.ThrowIfNull(timerIds);
        ArgumentNullException.ThrowIfNull(oscillators);ArgumentNullException.ThrowIfNull(oscillatorIds);
        ArgumentNullException.ThrowIfNull(physics);_physics=physics;
        _timers=timers;_oscillators=oscillators;
        var energySources=new List<EnergyBinding>();
        var oscillatorSources=new List<OscillatorBinding>();
        var timerSources=new List<TimerBinding>();
        var sources=new List<ScalarObservationSource>();var reads=new List<ScalarRead>();
        var keys=new HashSet<ScalarReadKey>();
        foreach(var part in parts)
        foreach(var declaration in part.ScalarObservations)
        {
            if(!Enum.IsDefined(declaration.Unit)||
                !part.RuntimeState.Any(state=>ReferenceEquals(state,declaration.Source.Cell)))
                throw new ArgumentException("Scalar observation requires supported units and an owned checkpoint cell.");
            var key=new ScalarReadKey(assembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot);
            if(!keys.Add(key))throw new ArgumentException("Duplicate scalar observation slot.");
            sources.Add(declaration.Source);reads.Add(new(key,declaration.Unit,declaration.Source.Read()));
        }
        foreach(var part in parts)
        foreach(var declaration in part.TimerObservations)
        {
            if(declaration.Timer.Owner!=part||declaration.Timer.Slot is null||
                !Enum.IsDefined(declaration.Quantity)||!timerIds.TryGetValue(declaration.Timer,out var id))
                throw new ArgumentException("Timer observation requires an owned declared timer and supported quantity.");
            var key=new ScalarReadKey(assembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot);
            if(!keys.Add(key))throw new ArgumentException("Duplicate scalar observation slot.");
            timerSources.Add(new(reads.Count,id,declaration.Quantity));
            reads.Add(new(key,ScalarUnit.Dimensionless,timers.ReadQuantity(id,declaration.Quantity)));
        }
        foreach(var part in parts)
        foreach(var declaration in part.OscillatorObservations)
        {
            if(declaration.Oscillator.Owner!=part||declaration.Oscillator.Slot is null||
                !Enum.IsDefined(declaration.Quantity)||!oscillatorIds.TryGetValue(declaration.Oscillator,out var id))
                throw new ArgumentException("Oscillator observation requires an owned declared oscillator and supported quantity.");
            var key=new ScalarReadKey(assembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot);
            if(!keys.Add(key))throw new ArgumentException("Duplicate scalar observation slot.");
            oscillatorSources.Add(new(reads.Count,id,declaration.Quantity));
            reads.Add(new(key,ScalarUnit.Dimensionless,oscillators.ReadQuantity(id,declaration.Quantity)));
        }
        foreach(var part in parts)
        foreach(var declaration in part.EnergyStoreObservations)
        {
            if(declaration.Body.Owner!=part||!Enum.IsDefined(declaration.Quantity)||
                !part.PhysicsEnergyStores.Any(store=>store.Body==declaration.Body))
                throw new ArgumentException("Energy observation requires an owned declared reservoir and supported quantity.");
            var body=assembly.QueryOwnerId(declaration.Body);
            var key=new ScalarReadKey(assembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot);
            if(!keys.Add(key))throw new ArgumentException("Duplicate scalar observation slot.");
            var unit=declaration.Quantity==EnergyStoreQuantity.FillFraction?ScalarUnit.Dimensionless:ScalarUnit.GameEnergy;
            energySources.Add(new(reads.Count,body,declaration.Quantity));
            reads.Add(new(key,unit,ReadEnergy(physics.EnergyStore(body),declaration.Quantity)));
        }
        _energySources=energySources.ToArray();
        _oscillatorSources=oscillatorSources.ToArray();
        _sources=sources.ToArray();_timerSources=timerSources.ToArray();_reads=reads.ToArray();
    }
    public ReadOnlySpan<ScalarRead> Capture()
    {
        for(var i=0;i<_sources.Length;i++)_reads[i]=_reads[i] with {Value=_sources[i].Read()};
        foreach(var source in _timerSources)
            _reads[source.ReadIndex]=_reads[source.ReadIndex] with {Value=_timers.ReadQuantity(source.Id,source.Quantity)};
        foreach(var source in _oscillatorSources)
            _reads[source.ReadIndex]=_reads[source.ReadIndex] with {Value=_oscillators.ReadQuantity(source.Id,source.Quantity)};
        foreach(var source in _energySources)
            _reads[source.ReadIndex]=_reads[source.ReadIndex] with {Value=ReadEnergy(_physics.EnergyStore(source.Body),source.Quantity)};
        return _reads;
    }
}
