using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public readonly record struct SceneEnumObservationKey<T>(MachinePart Owner,EnumObservationSlot<T> Slot) where T:unmanaged,Enum;
public abstract class SceneEnumObservation
{
    private protected SceneEnumObservation() { }
    internal abstract void Add(MachinePart owner,ScenePhysicsAssembly assembly,Dictionary<Type,SceneEnumObservations.IGroup> groups);
}
public sealed class SceneEnumObservation<T>:SceneEnumObservation where T:unmanaged,Enum
{
    public EnumObservationSlot<T> Slot { get; }
    public SimulationState<T> Cell { get; }
    public SceneEnumObservation(EnumObservationSlot<T> slot,SimulationState<T> cell)
    {ArgumentNullException.ThrowIfNull(cell);Slot=slot;Cell=cell;}
    internal override void Add(MachinePart owner,ScenePhysicsAssembly assembly,Dictionary<Type,SceneEnumObservations.IGroup> groups)
    {
        if(!owner.RuntimeState.Any(state=>ReferenceEquals(state,Cell)))
            throw new ArgumentException("Enum observation requires an owned checkpoint cell.");
        if(!groups.TryGetValue(typeof(T),out var group))
        {group=new SceneEnumObservations.Group<T>();groups.Add(typeof(T),group);}
        ((SceneEnumObservations.Group<T>)group).Add(new(assembly.QueryOwnerId(new(owner,MachinePart.RootBody)),Slot),Cell);
    }
}
internal sealed class SceneEnumObservations
{
    internal interface IGroup
    {
        void Register(CommittedPoseBuffer publication);
        void Stage(CommittedPoseBuffer publication);
    }
    internal sealed class Group<T>:IGroup where T:unmanaged,Enum
    {
        private readonly List<(EnumReadKey<T> Key,SimulationState<T> Cell)> _sources=[];
        private EnumRead<T>[] _reads=[];
        public void Add(EnumReadKey<T> key,SimulationState<T> cell)=>_sources.Add((key,cell));
        public void Register(CommittedPoseBuffer publication)
        {
            _reads=new EnumRead<T>[_sources.Count];Capture();publication.RegisterEnums<T>(_reads);
        }
        private void Capture()
        {
            for(var i=0;i<_sources.Count;i++)_reads[i]=new(_sources[i].Key,_sources[i].Cell.Value);
        }
        public void Stage(CommittedPoseBuffer publication){Capture();publication.StageEnums<T>(_reads);}
    }
    private readonly IGroup[] _groups;
    public SceneEnumObservations(IReadOnlyList<MachinePart> parts,ScenePhysicsAssembly assembly)
    {
        var groups=new Dictionary<Type,IGroup>();
        foreach(var part in parts)
        foreach(var declaration in part.EnumObservations)
        {
            ArgumentNullException.ThrowIfNull(declaration);declaration.Add(part,assembly,groups);
        }
        _groups=groups.Values.ToArray();
    }
    public void Register(CommittedPoseBuffer publication){foreach(var group in _groups)group.Register(publication);}
    public void Stage(CommittedPoseBuffer publication){foreach(var group in _groups)group.Stage(publication);}
}
