using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Bridge;

public readonly record struct PoseReadStamp(WorldGeneration Generation,SimulationRevision Revision,double SimulationTime);
public enum PoseSample { Previous, Current }

/// <summary>Single-thread publication: two committed buffers and one producer scratch buffer.
/// Pose and query declarations publish together. A lease pins the pair; stepping waits for release.
/// Reset retires old leases.</summary>
public sealed partial class CommittedPoseBuffer
{
    private enum Phase { Idle, Writing, Staged, Removed }
    private Phase _phase;
    private BodyPublicationRead[] _previous,_current,_pending;
    private SimulationCounterState[] _previousCounters,_currentCounters,_pendingCounters;
    private readonly Dictionary<SimulationCounterId,int> _counterIndices=new();
    private ElectricalInputRead[] _previousElectrical,_currentElectrical,_pendingElectrical;
    private readonly Dictionary<ElectricalInputKey,int> _electricalIndices=new();
    private BooleanRead[] _previousBooleans,_currentBooleans,_pendingBooleans;
    private readonly Dictionary<BooleanReadKey,int> _booleanIndices=new();
    private ScalarRead[] _previousScalars,_currentScalars,_pendingScalars;
    private readonly Dictionary<ScalarReadKey,int> _scalarIndices=new();
    private SimulationTimerState[] _previousTimers,_currentTimers,_pendingTimers;
    private readonly Dictionary<SimulationTimerId,int> _timerIndices=new();
    private PoseReadStamp _previousStamp,_currentStamp,_pendingStamp;
    private ulong _nextLease,_lease;
    private PhysicsMotionHistory[] _currentMotion=[],_pendingMotion=[];
    private int _pendingMotionCount;

    public CommittedPoseBuffer(PoseReadStamp stamp,ReadOnlySpan<BodyPublicationRead> initial,
        ReadOnlySpan<SimulationCounterState> counters,ReadOnlySpan<ElectricalInputRead> electrical,ReadOnlySpan<ScalarRead> scalars,ReadOnlySpan<SimulationTimerState> timers,ReadOnlySpan<BooleanRead> booleans)
    {
        ValidateStamp(stamp);
        _previous=initial.ToArray();_current=initial.ToArray();_pending=new BodyPublicationRead[initial.Length];
        for(var i=0;i<initial.Length;i++)
        {
            ValidateRead(initial[i],initial);
            if(initial[i].Pose.Id.Index!=i)throw new ArgumentException("Pose identities must match stable buffer indices.");
        }
        _previousCounters=counters.ToArray();_currentCounters=counters.ToArray();
        _pendingCounters=new SimulationCounterState[counters.Length];
        for(var i=0;i<counters.Length;i++)
        {
            ValidateCounter(counters[i]);
            if(!_counterIndices.TryAdd(counters[i].Id,i))throw new ArgumentException("Duplicate published counter identity.");
        }
        _previousElectrical=electrical.ToArray();_currentElectrical=electrical.ToArray();
        _pendingElectrical=new ElectricalInputRead[electrical.Length];
        for(var i=0;i<electrical.Length;i++)
        {
            electrical[i].Validate(initial);
            if(!_electricalIndices.TryAdd(electrical[i].Key,i))
                throw new ArgumentException("Duplicate published electrical input.");
        }
        _previousScalars=scalars.ToArray();_currentScalars=scalars.ToArray();_pendingScalars=new ScalarRead[scalars.Length];
        for(var i=0;i<scalars.Length;i++)
        {
            scalars[i].Validate(initial);
            if(!_scalarIndices.TryAdd(scalars[i].Key,i))throw new ArgumentException("Duplicate scalar observation.");
        }
        _previousTimers=timers.ToArray();_currentTimers=timers.ToArray();_pendingTimers=new SimulationTimerState[timers.Length];
        for(var i=0;i<timers.Length;i++)
        {
            ValidateTimer(timers[i]);
            if(!_timerIndices.TryAdd(timers[i].Id,i))throw new ArgumentException("Duplicate published timer identity.");
        }
        _previousBooleans=booleans.ToArray();_currentBooleans=booleans.ToArray();_pendingBooleans=new BooleanRead[booleans.Length];
        for(var i=0;i<booleans.Length;i++)
        {
            booleans[i].Validate(initial);
            if(!_booleanIndices.TryAdd(booleans[i].Key,i))throw new ArgumentException("Duplicate Boolean observation.");
        }
        _previousStamp=_currentStamp=stamp;
    }
    private static void ValidateTimer(SimulationTimerState state)
    {
        if(!Enum.IsDefined(state.Phase))throw new ArgumentException("Invalid committed timer phase.");
        if(state.Phase==SimulationTimerPhase.Ready&&state.StartedTick==-1&&state.DueTick==-1)return;
        if(state.StartedTick<0||state.DueTick<=state.StartedTick)
            throw new ArgumentException("Committed timer requires an initial ready state or a valid admitted interval.");
    }
    private static void ValidateCounter(SimulationCounterState state)
    {
        if(state.Target<1||state.Count<0||state.Count>state.Target)
            throw new ArgumentException("Invalid committed counter quantity.");
    }
    private static void ValidateStamp(PoseReadStamp stamp)
    {
        if(stamp.Generation.Value<=0||stamp.Revision.Value<0||!double.IsFinite(stamp.SimulationTime)||stamp.SimulationTime<0)
            throw new ArgumentException("Invalid pose publication stamp.");
    }
    private static void ValidateRead(BodyPublicationRead value,ReadOnlySpan<BodyPublicationRead> topology)
    {
        value.Query.Validate();value.Velocity.Validate();
        if(!Enum.IsDefined(value.Activity))throw new ArgumentException("Invalid committed owner activity.");
        var owner=value.Query.Owner;
        if(owner.Index>=topology.Length||topology[owner.Index].Pose.Id!=owner||
            topology[owner.Index].Query.Owner!=owner)
            throw new ArgumentException("Query owner must identify a root in the same publication.");
        if(value.Activity!=topology[owner.Index].Activity)
            throw new ArgumentException("Bodies belonging to one owner must publish the same activity.");
        var read=value.Pose;
        if(!Enum.IsDefined(read.MotionType)||!read.Pose.Center.IsFinite||!read.Pose.Rotation.IsValid||
            !read.ReferencePose.Center.IsFinite||!read.ReferencePose.Rotation.IsValid)
            throw new ArgumentException("Invalid pose read.");
    }
    public void RequireWritable()
    {
        if(_phase!=Phase.Idle||_lease!=0)throw new InvalidOperationException("Pose publication requires an idle unleased buffer.");
    }
    internal void BeginWrite(int motionSteps)
    {
        RequireWritable();
        if(motionSteps<0)throw new ArgumentOutOfRangeException(nameof(motionSteps));
        if(_pendingMotion.Length!=motionSteps)_pendingMotion=new PhysicsMotionHistory[motionSteps];
        _registrationClosed=true;
        _pendingMotionCount=0;_phase=Phase.Writing;
    }
    internal void AppendMotion(PhysicsMotionHistory motion)
    {
        ArgumentNullException.ThrowIfNull(motion);
        if(_phase!=Phase.Writing||_pendingMotionCount>=_pendingMotion.Length)
            throw new InvalidOperationException("No reserved motion publication capacity.");
        var start=_pendingMotionCount==0?_currentStamp.SimulationTime:_pendingMotion[_pendingMotionCount-1].EndTime;
        if(motion.StartTime!=start||motion.EndTime<=start||
            _pendingMotionCount>0&&motion.StepIndex!=checked(_pendingMotion[_pendingMotionCount-1].StepIndex+1))
            throw new ArgumentException("Motion steps must form one contiguous ordered interval.");
        _pendingMotion[_pendingMotionCount++]=motion;
    }
    internal void Stage(PoseReadStamp stamp,ReadOnlySpan<BodyPublicationRead> reads,
        ReadOnlySpan<SimulationCounterState> counters,ReadOnlySpan<ElectricalInputRead> electrical,ReadOnlySpan<ScalarRead> scalars,ReadOnlySpan<SimulationTimerState> timers,ReadOnlySpan<BooleanRead> booleans)
    {
        if(_phase!=Phase.Writing)throw new InvalidOperationException("Pose staging requires a reserved write.");
        ValidateStamp(stamp);
        if(stamp.Generation!=_currentStamp.Generation||stamp.Revision.Value!=checked(_currentStamp.Revision.Value+1)||
            stamp.SimulationTime<=_currentStamp.SimulationTime||reads.Length!=_current.Length)
            throw new ArgumentException("Pose publication must advance one revision within the same topology and generation.");
        if(booleans.Length!=_currentBooleans.Length)throw new ArgumentException("Boolean topology changed within a run.");
        for(var i=0;i<booleans.Length;i++)
        {
            booleans[i].Validate(reads);
            if(booleans[i].Key!=_currentBooleans[i].Key)throw new ArgumentException("Boolean identity or order changed within a run.");
        }
        if(timers.Length!=_currentTimers.Length)throw new ArgumentException("Timer topology changed within a run.");
        for(var i=0;i<timers.Length;i++)
        {
            ValidateTimer(timers[i]);
            if(timers[i].Id!=_currentTimers[i].Id)throw new ArgumentException("Timer identity or order changed within a run.");
        }
        if(scalars.Length!=_currentScalars.Length)throw new ArgumentException("Scalar topology changed within a run.");
        for(var i=0;i<scalars.Length;i++)
        {
            scalars[i].Validate(reads);
            if(scalars[i].Key!=_currentScalars[i].Key||scalars[i].Unit!=_currentScalars[i].Unit)
                throw new ArgumentException("Scalar identity, order or units changed within a run.");
        }
        if(electrical.Length!=_currentElectrical.Length)
            throw new ArgumentException("Electrical input topology changed within a run.");
        for(var i=0;i<electrical.Length;i++)
        {
            electrical[i].Validate(reads);
            if(electrical[i].Key!=_currentElectrical[i].Key)
                throw new ArgumentException("Electrical input identity or order changed within a run.");
        }
        if(counters.Length!=_currentCounters.Length)throw new ArgumentException("Counter topology changed within a run.");
        for(var i=0;i<counters.Length;i++)
        {
            ValidateCounter(counters[i]);
            if(counters[i].Id!=_currentCounters[i].Id||counters[i].Target!=_currentCounters[i].Target)
                throw new ArgumentException("Counter identity, order or target changed within a run.");
        }
        for(var i=0;i<reads.Length;i++)
        {
            ValidateRead(reads[i],reads);
            if(reads[i].Pose.Id!=_current[i].Pose.Id||reads[i].Query.Owner!=_current[i].Query.Owner)
                throw new ArgumentException("Body topology changed within a run.");
            var next=reads[i].Query;var previous=_current[i].Query;
            if(next.Revision.Value<previous.Revision.Value||
                next.Revision==previous.Revision&&next!=previous)
                throw new ArgumentException("Changed query declarations require an advancing collider revision.");
        }
        if(_pendingMotionCount!=_pendingMotion.Length)
            throw new ArgumentException("The complete reserved motion interval must be staged.");
        foreach(var motion in _pendingMotion)
        {
            if(motion.FinalPoses.Length!=reads.Length)throw new ArgumentException("Motion topology differs from pose publication.");
            for(var i=0;i<reads.Length;i++)
                if(motion.FinalPoses[i].Body!=reads[i].Pose.Id)throw new ArgumentException("Motion body order differs from pose publication.");
        }
        if(_pendingMotion.Length>0)
        {
            var final=_pendingMotion[^1];
            if(final.EndTime!=stamp.SimulationTime)throw new ArgumentException("Motion and pose timestamps differ.");
            for(var i=0;i<reads.Length;i++)
                if(final.FinalPoses[i].Pose!=reads[i].Pose.Pose)throw new ArgumentException("Motion endpoint differs from committed pose.");
        }
        ValidateEnumStage();
        ValidateContactStage(reads);
        booleans.CopyTo(_pendingBooleans);
        timers.CopyTo(_pendingTimers);
        scalars.CopyTo(_pendingScalars);
        electrical.CopyTo(_pendingElectrical);
        counters.CopyTo(_pendingCounters);
        reads.CopyTo(_pending);_pendingStamp=stamp;_phase=Phase.Staged;
    }
    internal void Publish()
    {
        if(_phase!=Phase.Staged)throw new InvalidOperationException("No staged pose publication.");
        PublishEnums();
        PublishContacts();
        var scratch=_previous;_previous=_current;_current=_pending;_pending=scratch;
        var booleanScratch=_previousBooleans;_previousBooleans=_currentBooleans;_currentBooleans=_pendingBooleans;_pendingBooleans=booleanScratch;
        var timerScratch=_previousTimers;_previousTimers=_currentTimers;_currentTimers=_pendingTimers;_pendingTimers=timerScratch;
        var scalarScratch=_previousScalars;_previousScalars=_currentScalars;_currentScalars=_pendingScalars;_pendingScalars=scalarScratch;
        var electricalScratch=_previousElectrical;_previousElectrical=_currentElectrical;
        _currentElectrical=_pendingElectrical;_pendingElectrical=electricalScratch;
        var counterScratch=_previousCounters;_previousCounters=_currentCounters;_currentCounters=_pendingCounters;_pendingCounters=counterScratch;
        var motionScratch=_currentMotion;_currentMotion=_pendingMotion;_pendingMotion=motionScratch;
        Array.Clear(_pendingMotion);_pendingMotionCount=0;
        _previousStamp=_currentStamp;_currentStamp=_pendingStamp;_phase=Phase.Idle;
    }
    internal void Discard()
    {
        if(_phase is not (Phase.Writing or Phase.Staged))throw new InvalidOperationException("No pending pose write.");
        DiscardEnums();
        DiscardContacts();
        Array.Clear(_pendingBooleans);
        Array.Clear(_pendingTimers);
        Array.Clear(_pendingScalars);
        Array.Clear(_pendingElectrical);
        Array.Clear(_pendingCounters);
        Array.Clear(_pending);Array.Clear(_pendingMotion);_pendingMotionCount=0;
        _phase=Phase.Idle;
    }
    internal void Remove()
    {
        if(_phase!=Phase.Idle)throw new InvalidOperationException("Pose removal requires an idle publication.");
        _previous=[];_current=[];_pending=[];_currentMotion=[];_pendingMotion=[];_pendingMotionCount=0;
        _previousCounters=[];_currentCounters=[];_pendingCounters=[];_counterIndices.Clear();
        _previousElectrical=[];_currentElectrical=[];_pendingElectrical=[];_electricalIndices.Clear();
        _previousScalars=[];_currentScalars=[];_pendingScalars=[];_scalarIndices.Clear();
        _previousBooleans=[];_currentBooleans=[];_pendingBooleans=[];_booleanIndices.Clear();
        _previousTimers=[];_currentTimers=[];_pendingTimers=[];_timerIndices.Clear();
        _enumChannels.Clear();
        RemoveContacts();
        _phase=Phase.Removed;
    }
    public PoseReadLease Acquire()
    {
        RequireWritable();
        _registrationClosed=true;
        _lease=_nextLease=checked(_nextLease+1);
        return new(this,_lease);
    }
    private void CheckLease(ulong token)
    {
        if(_phase!=Phase.Idle||token==0||token!=_lease)
            throw new InvalidOperationException("Pose lease is expired, retired or invalid.");
    }
    internal int TimerCount(ulong token) {CheckLease(token);return _currentTimers.Length;}
    internal SimulationTimerState Timer(ulong token,PoseSample sample,SimulationTimerId id)
    {
        CheckLease(token);
        if(!_timerIndices.TryGetValue(id,out var index))throw new ArgumentException("Timer is absent from this publication.");
        return sample switch
        {
            PoseSample.Previous=>_previousTimers[index],PoseSample.Current=>_currentTimers[index],
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal void CopyTimers(ulong token,PoseSample sample,Span<SimulationTimerState> target)
    {
        CheckLease(token);
        if(target.Length!=_currentTimers.Length)throw new ArgumentException("Timer target requires the complete topology.");
        switch(sample)
        {
            case PoseSample.Previous:_previousTimers.AsSpan().CopyTo(target);break;
            case PoseSample.Current:_currentTimers.AsSpan().CopyTo(target);break;
            default:throw new ArgumentOutOfRangeException(nameof(sample));
        }
    }
    internal int BooleanCount(ulong token) {CheckLease(token);return _currentBooleans.Length;}
    internal BooleanRead Boolean(ulong token,PoseSample sample,BooleanReadKey key)
    {
        CheckLease(token);
        if(!_booleanIndices.TryGetValue(key,out var index))throw new ArgumentException("Boolean observation is absent.");
        return sample switch
        {
            PoseSample.Previous=>_previousBooleans[index],PoseSample.Current=>_currentBooleans[index],
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal void CopyBooleans(ulong token,PoseSample sample,Span<BooleanRead> target)
    {
        CheckLease(token);
        if(target.Length!=_currentBooleans.Length)throw new ArgumentException("Boolean target requires the complete topology.");
        switch(sample)
        {
            case PoseSample.Previous:_previousBooleans.AsSpan().CopyTo(target);break;
            case PoseSample.Current:_currentBooleans.AsSpan().CopyTo(target);break;
            default:throw new ArgumentOutOfRangeException(nameof(sample));
        }
    }
    internal int ScalarCount(ulong token) {CheckLease(token);return _currentScalars.Length;}
    internal ScalarRead Scalar(ulong token,PoseSample sample,ScalarReadKey key)
    {
        CheckLease(token);
        if(!_scalarIndices.TryGetValue(key,out var index))throw new ArgumentException("Scalar observation is absent.");
        return sample switch
        {
            PoseSample.Previous=>_previousScalars[index],PoseSample.Current=>_currentScalars[index],
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal void CopyScalars(ulong token,PoseSample sample,Span<ScalarRead> target)
    {
        CheckLease(token);
        if(target.Length!=_currentScalars.Length)throw new ArgumentException("Scalar target requires the complete topology.");
        switch(sample)
        {
            case PoseSample.Previous:_previousScalars.AsSpan().CopyTo(target);break;
            case PoseSample.Current:_currentScalars.AsSpan().CopyTo(target);break;
            default:throw new ArgumentOutOfRangeException(nameof(sample));
        }
    }
    internal void CopyElectricalInputs(ulong token,PoseSample sample,Span<ElectricalInputRead> target)
    {
        CheckLease(token);
        if(target.Length!=_currentElectrical.Length)
            throw new ArgumentException("Electrical target must have the complete topology size.");
        switch(sample)
        {
            case PoseSample.Previous:_previousElectrical.AsSpan().CopyTo(target);break;
            case PoseSample.Current:_currentElectrical.AsSpan().CopyTo(target);break;
            default:throw new ArgumentOutOfRangeException(nameof(sample));
        }
    }
    internal int ElectricalInputCount(ulong token) {CheckLease(token);return _currentElectrical.Length;}
    internal ElectricalInputRead ElectricalInput(ulong token,PoseSample sample,ElectricalInputKey key)
    {
        CheckLease(token);
        if(!_electricalIndices.TryGetValue(key,out var index))
            throw new ArgumentException("Electrical input is absent from this publication.",nameof(key));
        return sample switch
        {
            PoseSample.Previous=>_previousElectrical[index],PoseSample.Current=>_currentElectrical[index],
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal int CounterCount(ulong token) {CheckLease(token);return _currentCounters.Length;}
    internal SimulationCounterState Counter(ulong token,PoseSample sample,SimulationCounterId id)
    {
        CheckLease(token);
        if(!_counterIndices.TryGetValue(id,out var index))throw new ArgumentException("Counter is absent from this publication.",nameof(id));
        return sample switch
        {
            PoseSample.Previous=>_previousCounters[index],PoseSample.Current=>_currentCounters[index],
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal int Count(ulong token) {CheckLease(token);return _current.Length;}
    internal int MotionCount(ulong token) {CheckLease(token);return _currentMotion.Length;}
    internal PhysicsMotionHistory Motion(ulong token,int index) {CheckLease(token);return _currentMotion[index];}
    internal PoseReadStamp Stamp(ulong token,PoseSample sample)
    {
        CheckLease(token);
        return sample switch
        {
            PoseSample.Previous=>_previousStamp,PoseSample.Current=>_currentStamp,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal BodyPoseRead Read(ulong token,PoseSample sample,int index)
    {
        CheckLease(token);
        return sample switch
        {
            PoseSample.Previous=>_previous[index].Pose,PoseSample.Current=>_current[index].Pose,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal BodyQueryRead Query(ulong token,PoseSample sample,int index)
    {
        CheckLease(token);
        return sample switch
        {
            PoseSample.Previous=>_previous[index].Query,PoseSample.Current=>_current[index].Query,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal BodyVelocityRead Velocity(ulong token,PoseSample sample,PhysicsBodyId id)
    {
        CheckLease(token);
        if(id.Index>=_current.Length)throw new ArgumentOutOfRangeException(nameof(id));
        return sample switch
        {
            PoseSample.Previous=>_previous[id.Index].Velocity,PoseSample.Current=>_current[id.Index].Velocity,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal OwnerActivity Activity(ulong token,PoseSample sample,int index)
    {
        CheckLease(token);
        return sample switch
        {
            PoseSample.Previous=>_previous[index].Activity,PoseSample.Current=>_current[index].Activity,
            _=>throw new ArgumentOutOfRangeException(nameof(sample))
        };
    }
    internal void Copy(ulong token,PoseSample sample,Span<BodyPoseRead> target)
    {
        CheckLease(token);
        if(target.Length!=_current.Length)throw new ArgumentException("Pose target must have the complete topology size.");
        switch(sample)
        {
            case PoseSample.Previous:for(var i=0;i<target.Length;i++)target[i]=_previous[i].Pose;break;
            case PoseSample.Current:for(var i=0;i<target.Length;i++)target[i]=_current[i].Pose;break;
            default:throw new ArgumentOutOfRangeException(nameof(sample));
        }
    }
    internal void Release(ulong token)
    {
        if(token==0||token!=_lease)throw new InvalidOperationException("Pose lease was already released or is invalid.");
        _lease=0;
    }
}

/// <summary>Explicit single-reader lease. Individual returned values may be retained.
/// A copied lease shares its lifetime and cannot release it twice.</summary>
public readonly struct PoseReadLease : IDisposable
{
    private readonly CommittedPoseBuffer? _owner;
    private readonly ulong _token;
    internal PoseReadLease(CommittedPoseBuffer owner,ulong token) {_owner=owner;_token=token;}
    private CommittedPoseBuffer Owner=>_owner??throw new InvalidOperationException("Uninitialized pose lease.");
    public int Count=>Owner.Count(_token);
    public int CounterCount=>Owner.CounterCount(_token);
    public int TimerCount=>Owner.TimerCount(_token);
    public SimulationTimerState ReadTimer(PoseSample sample,SimulationTimerId id)=>Owner.Timer(_token,sample,id);
    public void CopyTimers(PoseSample sample,Span<SimulationTimerState> target)=>Owner.CopyTimers(_token,sample,target);
    public int EnumCount<T>() where T:unmanaged,Enum=>Owner.EnumCount<T>(_token);
    public EnumRead<T> ReadEnum<T>(PoseSample sample,EnumReadKey<T> key) where T:unmanaged,Enum=>Owner.ReadEnum(_token,sample,key);
    public void CopyEnums<T>(PoseSample sample,Span<EnumRead<T>> target) where T:unmanaged,Enum=>Owner.CopyEnums(_token,sample,target);
    public int CompliantContactCount=>Owner.ContactCount(_token);
    public CompliantContactState ReadCompliantContact(PoseSample sample,CompliantContactKey key)=>Owner.Contact(_token,sample,key);
    public void CopyCompliantContacts(PoseSample sample,Span<CompliantContactState> target)=>Owner.CopyContacts(_token,sample,target);
    public int BooleanCount=>Owner.BooleanCount(_token);
    public BooleanRead ReadBoolean(PoseSample sample,BooleanReadKey key)=>Owner.Boolean(_token,sample,key);
    public void CopyBooleans(PoseSample sample,Span<BooleanRead> target)=>Owner.CopyBooleans(_token,sample,target);
    public int ScalarCount=>Owner.ScalarCount(_token);
    public ScalarRead ReadScalar(PoseSample sample,ScalarReadKey key)=>Owner.Scalar(_token,sample,key);
    public void CopyScalars(PoseSample sample,Span<ScalarRead> target)=>Owner.CopyScalars(_token,sample,target);
    public int ElectricalInputCount=>Owner.ElectricalInputCount(_token);
    public void CopyElectricalInputs(PoseSample sample,Span<ElectricalInputRead> target)=>Owner.CopyElectricalInputs(_token,sample,target);
    public ElectricalInputRead ReadElectricalInput(PoseSample sample,ElectricalInputKey key)=>Owner.ElectricalInput(_token,sample,key);
    public SimulationCounterState ReadCounter(PoseSample sample,SimulationCounterId id)=>Owner.Counter(_token,sample,id);
    public int MotionStepCount=>Owner.MotionCount(_token);
    public PhysicsMotionHistory ReadMotionStep(int index)=>Owner.Motion(_token,index);
    public RigidPose SampleAcceptedPose(PhysicsBodyId body,double simulationTime)
    {
        var previous=Stamp(PoseSample.Previous);var current=Stamp(PoseSample.Current);
        if(body.Index>=Count)throw new ArgumentOutOfRangeException(nameof(body));
        if(!double.IsFinite(simulationTime)||simulationTime<previous.SimulationTime||simulationTime>current.SimulationTime)
            throw new ArgumentOutOfRangeException(nameof(simulationTime));
        if(simulationTime==current.SimulationTime)return Read(PoseSample.Current,body.Index).Pose;
        if(simulationTime==previous.SimulationTime)return Read(PoseSample.Previous,body.Index).Pose;
        for(var i=MotionStepCount-1;i>=0;i--)
        {
            var motion=ReadMotionStep(i);
            if(simulationTime>=motion.StartTime&&simulationTime<=motion.EndTime)return motion.Sample(body,simulationTime);
        }
        throw new InvalidOperationException("No accepted motion covers this publication time.");
    }
    public PoseReadStamp Stamp(PoseSample sample)=>Owner.Stamp(_token,sample);
    public BodyPoseRead Read(PoseSample sample,int index)=>Owner.Read(_token,sample,index);
    public BodyVelocityRead ReadVelocity(PoseSample sample,PhysicsBodyId id)=>Owner.Velocity(_token,sample,id);
    public OwnerActivity ReadActivity(PoseSample sample,int index)=>Owner.Activity(_token,sample,index);
    public BodyQueryRead ReadQuery(PoseSample sample,int index)=>Owner.Query(_token,sample,index);
    /// <summary>Sample body and reference from the same accepted timeline.
    /// The binding must match the publication's committed reference frame.</summary>
    public BodyPoseRead SamplePose(double simulationTime,int index,PoseReferenceBinding reference)
    {
        reference.Validate();
        var current=Read(PoseSample.Current,index);
        var referenceEnd=reference.Resolve(reference.Kind==PoseReferenceKind.Body
            ?Read(PoseSample.Current,reference.Body.Index).Pose:RigidPose.Identity);
        if(referenceEnd!=current.ReferencePose)throw new ArgumentException("Reference binding does not match this publication.");
        var pose=SampleAcceptedPose(current.Id,simulationTime);
        if(simulationTime==Stamp(PoseSample.Current).SimulationTime)return current;
        var frame=reference.Resolve(reference.Kind==PoseReferenceKind.Body
            ?SampleAcceptedPose(reference.Body,simulationTime):RigidPose.Identity);
        return new(current.Id,current.MotionType,pose,frame);
    }
    /// <summary>Trace accepted motion at a pinned presentation time. Discrete query declarations
    /// hold their previous value until the current commit timestamp. Exclusions identify owner roots.
    /// Range and result are ray parameters, not distances for non-unit directions.</summary>
    public double Trace(double simulationTime,TraceMedium medium,CollisionVector origin,CollisionVector direction,
        double range,PhysicsBodyId? emitter=null,PhysicsBodyId? receiver=null)
    {
        var previous=Stamp(PoseSample.Previous);var current=Stamp(PoseSample.Current);
        if(!double.IsFinite(simulationTime)||simulationTime<previous.SimulationTime||simulationTime>current.SimulationTime)
            throw new ArgumentOutOfRangeException(nameof(simulationTime));
        var sample=simulationTime==current.SimulationTime?PoseSample.Current:PoseSample.Previous;
        if(!Enum.IsDefined(medium))throw new ArgumentOutOfRangeException(nameof(medium));
        ValidateOwner(emitter);ValidateOwner(receiver);
        var trace=new PointTrace(origin,direction,range);
        for(var i=0;i<Count;i++)
        {
            var query=ReadQuery(sample,i);
            if(query.Participation==CollisionParticipation.Disabled||query.Owner==emitter||query.Owner==receiver)continue;
            var geometry=medium switch
            {
                TraceMedium.Light or TraceMedium.Sound=>query.Opaque,
                TraceMedium.Air=>query.Solid,
                _=>throw new ArgumentOutOfRangeException(nameof(medium))
            };
            if(geometry is not null)trace.Test(geometry,SampleAcceptedPose(new(i),simulationTime));
        }
        return trace.Closest;
    }
    private void ValidateOwner(PhysicsBodyId? owner)
    {
        if(owner is { } id&&(id.Index>=Count||ReadQuery(PoseSample.Current,id.Index).Owner!=id))
            throw new ArgumentException("Trace exclusion must identify an owner root in this publication.");
    }
    public void Copy(PoseSample sample,Span<BodyPoseRead> target)=>Owner.Copy(_token,sample,target);
    public void Dispose()=>_owner?.Release(_token);
}
