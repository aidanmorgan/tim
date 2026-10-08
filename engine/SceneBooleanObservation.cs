using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public enum BooleanObservationStorage { Cell, OpticalControl }
public readonly record struct BooleanObservationSource
{
    public BooleanObservationStorage Storage { get; }
    private readonly SimulationState<bool>? _cell;
    private readonly OpticalLogicControl? _control;
    private readonly OpticalControlQuantity _quantity;
    public BooleanObservationSource(SimulationState<bool> cell)
    {ArgumentNullException.ThrowIfNull(cell);Storage=BooleanObservationStorage.Cell;_cell=cell;}
    public BooleanObservationSource(OpticalLogicControl control,OpticalControlQuantity quantity)
    {
        ArgumentNullException.ThrowIfNull(control);
        if(!Enum.IsDefined(quantity))throw new ArgumentOutOfRangeException(nameof(quantity));
        Storage=BooleanObservationStorage.OpticalControl;_control=control;_quantity=quantity;
    }
    internal SimulationTransactionParticipant Owner=>Storage switch
    {
        BooleanObservationStorage.Cell=>_cell??throw new ArgumentException("Missing Boolean cell."),
        BooleanObservationStorage.OpticalControl=>_control??throw new ArgumentException("Missing optical controller."),
        _=>throw new ArgumentException("Unsupported Boolean source.")
    };
    internal bool Read()
    {
        _=Owner;
        return Storage switch
        {
            BooleanObservationStorage.Cell=>_cell!.Value,
            BooleanObservationStorage.OpticalControl=>_control!.Read(_quantity),
            _=>throw new InvalidOperationException("Unsupported Boolean source.")
        };
    }
}
public readonly record struct SceneBooleanObservation(BooleanObservationSlot Slot,BooleanObservationSource Source);
public readonly record struct SceneBooleanObservationKey(MachinePart Owner,BooleanObservationSlot Slot);

/// <summary>Run-owned scratch; publication copies it as part of the complete gameplay transaction.</summary>
internal sealed class SceneBooleanObservations
{
    private readonly BooleanObservationSource[] _sources;
    private readonly BooleanRead[] _reads;
    public SceneBooleanObservations(IReadOnlyList<MachinePart> parts,ScenePhysicsAssembly assembly)
    {
        var sources=new List<BooleanObservationSource>();var reads=new List<BooleanRead>();var keys=new HashSet<BooleanReadKey>();
        foreach(var part in parts)
        foreach(var declaration in part.BooleanObservations)
        {
            if(!part.RuntimeState.Any(state=>ReferenceEquals(state,declaration.Source.Owner)))
                throw new ArgumentException("Boolean source must be an owned transaction participant.");
            var key=new BooleanReadKey(assembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot);
            if(!keys.Add(key))throw new ArgumentException("Duplicate Boolean observation.");
            sources.Add(declaration.Source);reads.Add(new(key,declaration.Source.Read()));
        }
        _sources=sources.ToArray();_reads=reads.ToArray();
    }
    public ReadOnlySpan<BooleanRead> Capture()
    {
        for(var i=0;i<_sources.Length;i++)_reads[i]=_reads[i] with {Value=_sources[i].Read()};
        return _reads;
    }
}
