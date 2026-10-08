using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

public readonly record struct EnumObservationSlot<T> where T:unmanaged,Enum
{
    public int Index { get; }
    public EnumObservationSlot(int index)
    {if(index<0)throw new ArgumentOutOfRangeException(nameof(index));Index=index;}
}
public readonly record struct EnumReadKey<T>(PhysicsBodyId Owner,EnumObservationSlot<T> Slot) where T:unmanaged,Enum;
public readonly record struct EnumRead<T>(EnumReadKey<T> Key,T Value) where T:unmanaged,Enum
{
    internal void Validate(ReadOnlySpan<BodyPublicationRead> bodies)
    {
        if(Key.Owner.Index>=bodies.Length||bodies[Key.Owner.Index].Query.Owner!=Key.Owner)
            throw new ArgumentException("Enum observation requires a root owner.");
        if(!Enum.IsDefined(Value))throw new ArgumentException("Undefined enum observation value.");
    }
}

/// <summary>Typed channels share the pose publication's transaction, topology freeze and lease.</summary>
public sealed partial class CommittedPoseBuffer
{
    private interface IEnumChannel
    {
        bool Staged { get; }
        void Publish();
        void Discard();
    }
    private sealed class EnumChannel<T>:IEnumChannel where T:unmanaged,Enum
    {
        private EnumRead<T>[] _previous,_current,_pending;
        private readonly Dictionary<EnumReadKey<T>,int> _indices=[];
        public bool Staged { get; private set; }
        public int Count=>_current.Length;
        public EnumChannel(ReadOnlySpan<EnumRead<T>> values,ReadOnlySpan<BodyPublicationRead> bodies)
        {
            for(var i=0;i<values.Length;i++)
            {
                values[i].Validate(bodies);
                if(!_indices.TryAdd(values[i].Key,i))throw new ArgumentException("Duplicate enum observation.");
            }
            _previous=values.ToArray();_current=values.ToArray();_pending=new EnumRead<T>[values.Length];
        }
        public void Stage(ReadOnlySpan<EnumRead<T>> values,ReadOnlySpan<BodyPublicationRead> bodies)
        {
            if(Staged)throw new InvalidOperationException("Enum channel already staged for this tick.");
            if(values.Length!=_current.Length)throw new ArgumentException("Enum topology changed within a run.");
            for(var i=0;i<values.Length;i++)
            {
                values[i].Validate(bodies);
                if(values[i].Key!=_current[i].Key)throw new ArgumentException("Enum identity or order changed within a run.");
            }
            values.CopyTo(_pending);Staged=true;
        }
        public void Publish()
        {
            var scratch=_previous;_previous=_current;_current=_pending;_pending=scratch;Staged=false;
        }
        public void Discard(){Array.Clear(_pending);Staged=false;}
        private ReadOnlySpan<EnumRead<T>> Values(PoseSample sample)=>sample switch
        {
            PoseSample.Previous=>_previous,PoseSample.Current=>_current,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
        public EnumRead<T> Read(PoseSample sample,EnumReadKey<T> key)
        {
            if(!_indices.TryGetValue(key,out var index))throw new ArgumentException("Enum observation is absent.");
            return Values(sample)[index];
        }
        public void Copy(PoseSample sample,Span<EnumRead<T>> target)
        {
            if(target.Length!=Count)throw new ArgumentException("Enum target requires the complete topology.");
            Values(sample).CopyTo(target);
        }
    }
    private readonly Dictionary<Type,IEnumChannel> _enumChannels=[];
    private bool _registrationClosed;
    public void RegisterEnums<T>(ReadOnlySpan<EnumRead<T>> initial) where T:unmanaged,Enum
    {
        RequireWritable();
        if(_registrationClosed)throw new InvalidOperationException("Enum topology is frozen after the first lease or write.");
        if(_enumChannels.ContainsKey(typeof(T)))throw new ArgumentException("Enum type already registered.");
        var channel=new EnumChannel<T>(initial,_current);
        _enumChannels.Add(typeof(T),channel);
    }
    private EnumChannel<T> EnumChannelFor<T>() where T:unmanaged,Enum=>
        _enumChannels.TryGetValue(typeof(T),out var channel)?(EnumChannel<T>)channel:
            throw new ArgumentException("Enum type is not registered in this publication.");
    internal void StageEnums<T>(ReadOnlySpan<EnumRead<T>> values) where T:unmanaged,Enum
    {
        if(_phase!=Phase.Writing)throw new InvalidOperationException("Enum staging requires a reserved pose write.");
        EnumChannelFor<T>().Stage(values,_current);
    }
    private void ValidateEnumStage()
    {
        foreach(var channel in _enumChannels.Values)
            if(!channel.Staged)throw new InvalidOperationException("Every enum channel must be staged with the complete tick.");
    }
    private void PublishEnums(){foreach(var channel in _enumChannels.Values)channel.Publish();}
    private void DiscardEnums(){foreach(var channel in _enumChannels.Values)channel.Discard();}
    internal int EnumCount<T>(ulong token) where T:unmanaged,Enum
    {CheckLease(token);return EnumChannelFor<T>().Count;}
    internal EnumRead<T> ReadEnum<T>(ulong token,PoseSample sample,EnumReadKey<T> key) where T:unmanaged,Enum
    {CheckLease(token);return EnumChannelFor<T>().Read(sample,key);}
    internal void CopyEnums<T>(ulong token,PoseSample sample,Span<EnumRead<T>> target) where T:unmanaged,Enum
    {CheckLease(token);EnumChannelFor<T>().Copy(sample,target);}
}
