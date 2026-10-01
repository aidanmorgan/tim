namespace CuriousContraptions.WireDesign;

public enum RevisionTwoCase
{
    AxisX,AxisY,AxisZ,UnknownAxis,ExtentGolden,ExtentMinimumReject,ExtentFractionReject,
    ColourEndpoints,ColourAlphaReject,ColourNonfiniteReject,ReciprocalDataFull,
    ReciprocalControlFull,SameAllocationReturn,StaleReturn,PendingResponseProgress,
    MaximumControlShapes,ReturnReceiptsFit,BarrierIdentity,BarrierAcknowledgements,
    QueryWindow,AnimationRequestOwner,AnimationDefinitionDuplicate,AnimationResultRelease,
    Animation2516,Animation2517,Animation4096,HiddenOutputReservation,PhysicalVelocityBudget,
    ResultWidths,QueryWidths,DescriptorWidths,OriginalGenerationZero,
    RangePositive,RangeReversed,RangeEqual,RangeOverflow,AngleOverflow,
    SampleActive,SampleRetiredReuse,SamplePending,SampleFailed,SampleDuplicateRemove,
    SampleReset,SampleReordered,PresentedOldOccurrence,QueryDelayedRevision,
    QueryResetStamp,QueryCancelledStamp,QueryDuplicateRelease,QueryRejected,
    QueryBarrierCancelled,QueryRevisionStale,QueryCompletedBarrier,
    WaveEarlyReceipt,WaveEarlyContinues,WaveRangeBoundary,WaveLateHeld,
    WaveDelayedReceipt,WaveLostReceipt
}
public sealed record RevisionTwoResult(RevisionTwoCase Case,bool Passed,string Detail);
public enum Axis : ushort { X=1,Y=2,Z=3 }
public enum LeaseCategory { Data,Control }
public enum LeasePhase { Outbound,Returning,Free }
public readonly record struct BufferIdentity(ulong Value);
public readonly record struct RequestIdentity(Generation Generation,CommandSequence Sequence);
public readonly record struct Colour(double Red,double Green,double Blue,double Alpha);
public readonly record struct Vector(double X,double Y,double Z);

public static class RevisionTwoOracles
{
    public const int DataBytes=504*1024,ControlBytes=8*1024,MaximumOutputs=2516;
    private sealed record Lease(int Bytes,LeaseCategory Category,LeasePhase Phase);
    private sealed class Edge
    {
        private readonly Dictionary<BufferIdentity,Lease> _leases=[];
        public int Bytes=>_leases.Values.Sum(v=>v.Bytes);
        public int InFlight=>_leases.Values.Count(v=>v.Phase!=LeasePhase.Free);
        public void Allocate(BufferIdentity id,int bytes,LeaseCategory category)
        {
            if(id.Value==0||bytes<94||bytes>65536||!Enum.IsDefined(category)||_leases.ContainsKey(id))
                throw new ArgumentException("Invalid lease.");
            var same=_leases.Values.Where(v=>v.Category==category).ToArray();
            var byteLimit=category==LeaseCategory.Data?DataBytes:ControlBytes;
            var slotLimit=category==LeaseCategory.Data?56:8;
            if(same.Length==slotLimit||same.Sum(v=>v.Bytes)+bytes>byteLimit||
                category==LeaseCategory.Control&&bytes>1024)throw new ArgumentException("Lease capacity.");
            _leases.Add(id,new(bytes,category,LeasePhase.Outbound));
        }
        public void BeginReturn(BufferIdentity id)
        {
            var lease=_leases[id];
            if(lease.Phase!=LeasePhase.Outbound)throw new ArgumentException("Unexpected return.");
            _leases[id]=lease with{Phase=LeasePhase.Returning};
        }
        public void CompleteReturn(BufferIdentity id,int bytes)
        {
            if(!_leases.TryGetValue(id,out var lease)||lease.Phase!=LeasePhase.Returning||lease.Bytes!=bytes)
                throw new ArgumentException("Invalid return identity or allocation.");
            _leases[id]=lease with{Phase=LeasePhase.Free};
        }
        public void Reuse(BufferIdentity old,BufferIdentity next)
        {
            if(!_leases.TryGetValue(old,out var lease)||lease.Phase!=LeasePhase.Free||
                next.Value==0||_leases.ContainsKey(next))throw new ArgumentException("Invalid reuse.");
            _leases.Remove(old);_leases.Add(next,lease with{Phase=LeasePhase.Outbound});
        }
    }
    private enum RequestPhase { Pending,Result }
    private sealed class Window(int capacity)
    {
        private readonly Dictionary<RequestIdentity,RequestPhase> _entries=[];
        public int Count=>_entries.Count;
        public bool Admit(RequestIdentity id)
        {
            if(id.Generation.Value<=0||id.Sequence.Value==0)throw new ArgumentException("Invalid original identity.");
            if(_entries.ContainsKey(id))return false;
            if(Count==capacity)throw new ArgumentException("Window full.");
            _entries.Add(id,RequestPhase.Pending);return true;
        }
        public void Commit(RequestIdentity id)=>_entries[id]=RequestPhase.Result;
        public void Acknowledge(Generation generation,CommandSequence highest)
        {
            var records=_entries.Where(v=>v.Key.Generation==generation&&v.Key.Sequence.Value<=highest.Value).ToArray();
            if(records.Any(v=>v.Value!=RequestPhase.Result))throw new ArgumentException("Pending gap.");
            foreach(var entry in records)_entries.Remove(entry.Key);
        }
    }
    public static Vector Direction(Axis axis)=>axis switch
    {
        Axis.X=>new(1,0,0),Axis.Y=>new(0,1,0),Axis.Z=>new(0,0,1),
        _=>throw new ArgumentException("Unknown axis.")
    };
    public static (double Scale,double Offset) Extent(Axis axis,double anchor,double minimum,double initial,double fraction)
    {
        _=Direction(axis);
        if(!double.IsFinite(anchor)||Math.Abs(anchor)>1e6||!double.IsFinite(minimum)||minimum<1e-6||minimum>1||
            !double.IsFinite(initial)||initial<0||initial>1||!double.IsFinite(fraction)||fraction<0||fraction>1)
            throw new ArgumentException("Invalid extent.");
        var a=Math.Max(minimum,initial);var b=Math.Max(minimum,fraction);
        return (b/a,anchor*(a-b)/a);
    }
    public static void Colours(Colour from,Colour to,double baselineAlpha)
    {
        static bool Valid(Colour c)=>double.IsFinite(c.Red)&&double.IsFinite(c.Green)&&double.IsFinite(c.Blue)&&
            double.IsFinite(c.Alpha)&&c.Alpha>=0&&c.Alpha<=1;
        if(!Valid(from)||!Valid(to)||from.Alpha!=baselineAlpha||to.Alpha!=baselineAlpha||
            !float.IsFinite((float)(to.Red-from.Red))||!float.IsFinite((float)(to.Green-from.Green))||
            !float.IsFinite((float)(to.Blue-from.Blue)))throw new ArgumentException("Invalid colour endpoints.");
    }
    public static void ReserveOutputs(int registered,int added)
    {
        if(registered<0||added<0||checked((long)registered+added)>MaximumOutputs)
            throw new ArgumentException("Complete output exceeds sample capacity.");
    }
    private readonly record struct InstanceIdentity(ulong Value);
    private readonly record struct DescriptorIdentity(ulong Value);
    private readonly record struct TargetIdentity(ulong Value);
    private readonly record struct Revision(ulong Value);
    private enum Property : ushort { Rotation=1,PropagationDistance=9 }
    private enum QueryOutcome : ushort { Found=1,Empty=2,Stale=3,Unsupported=4,Cancelled=5 }
    private sealed record Sample(Generation Generation,InstanceIdentity Instance,TargetIdentity Target,
        Property Property,CommandSequence Publication,double Value);
    private sealed record ActiveBinding(Generation Generation,InstanceIdentity Instance,TargetIdentity Target,
        Property Property,DescriptorIdentity Descriptor,CommandSequence LastPublication);
    private sealed record QueryStamp(RequestIdentity Identity,Revision Required,QueryOutcome Outcome);
    private static bool CanApply(Sample sample,ActiveBinding? active)=>active is not null&&
        sample.Generation==active.Generation&&sample.Instance==active.Instance&&
        sample.Target==active.Target&&sample.Property==active.Property&&
        Enum.IsDefined(sample.Property)&&double.IsFinite(sample.Value)&&
        sample.Publication.Value>active.LastPublication.Value;
    private static void ScalarRange(double from,double to,double angleFrom=0,double angleTo=1)
    {
        if(!double.IsFinite(from)||!double.IsFinite(to)||to<=from||!double.IsFinite(to-from)||
            !double.IsFinite(angleFrom)||!double.IsFinite(angleTo)||!double.IsFinite(angleTo-angleFrom))
            throw new ArgumentException("Invalid finite mapping range.");
    }
    private enum QueryTransition { RevisionUnavailable,Barrier }
    private static QueryStamp ResolveQuery(QueryStamp request,bool completed,QueryTransition transition)
    {
        if(!Enum.IsDefined(transition))throw new ArgumentException("Unknown query transition.");
        return completed?request:request with{Outcome=transition==QueryTransition.Barrier?
            QueryOutcome.Cancelled:QueryOutcome.Stale};
    }
    private static (double Distance,bool Complete) Wave(double age,bool presented,double minimum)
    {
        if(!double.IsFinite(age)||age<0||!double.IsFinite(minimum)||minimum<0||minimum>=8)
            throw new ArgumentException("Invalid wavefront state.");
        var distance=Math.Min(8,age*12);
        return distance>=8?(presented?(8,true):(minimum,false)):(distance,false);
    }
    public static IReadOnlyList<RevisionTwoResult> Run()
    {
        var results=new List<RevisionTwoResult>();
        void Require(bool value){if(!value)throw new InvalidOperationException("Oracle mismatch.");}
        void Reject(Action action)
        {
            try{action();}catch(ArgumentException){return;}
            throw new InvalidOperationException("Invalid fixture accepted.");
        }
        void Check(RevisionTwoCase id,Action action)
        {
            try{action();results.Add(new(id,true,"Passed"));}
            catch(Exception error){results.Add(new(id,false,error.Message));}
        }
        Check(RevisionTwoCase.AxisX,()=>Require(Direction(Axis.X)==new Vector(1,0,0)));
        Check(RevisionTwoCase.AxisY,()=>Require(Direction(Axis.Y)==new Vector(0,1,0)));
        Check(RevisionTwoCase.AxisZ,()=>Require(Direction(Axis.Z)==new Vector(0,0,1)));
        Check(RevisionTwoCase.UnknownAxis,()=>Reject(()=>Direction((Axis)0)));
        Check(RevisionTwoCase.ExtentGolden,()=>Require(Extent(Axis.Y,-.5,.1,1,.25)==(.25,-.375)));
        Check(RevisionTwoCase.ExtentMinimumReject,()=>Reject(()=>Extent(Axis.Y,-.5,0,1,.25)));
        Check(RevisionTwoCase.ExtentFractionReject,()=>Reject(()=>Extent(Axis.Y,-.5,.1,1,1.01)));
        Check(RevisionTwoCase.ColourEndpoints,()=>Colours(new(.1,.2,.3,.5),new(.6,.7,.8,.5),.5));
        Check(RevisionTwoCase.ColourAlphaReject,()=>Reject(()=>Colours(new(.1,.2,.3,.4),new(.6,.7,.8,.5),.5)));
        Check(RevisionTwoCase.ColourNonfiniteReject,()=>Reject(()=>Colours(new(double.NaN,0,0,1),new(0,0,0,1),1)));
        Edge Full()
        {
            var edge=new Edge();
            for(ulong i=1;i<=7;i++)edge.Allocate(new(i),65536,LeaseCategory.Data);
            edge.Allocate(new(8),57344,LeaseCategory.Data);
            return edge;
        }
        var ab=Full();var ba=Full();
        Check(RevisionTwoCase.ReciprocalDataFull,()=>
        {Require(ab.Bytes==DataBytes&&ba.Bytes==DataBytes);Reject(()=>ab.Allocate(new(9),94,LeaseCategory.Data));});
        Check(RevisionTwoCase.ReciprocalControlFull,()=>
        {
            for(ulong i=20;i<28;i++){ab.Allocate(new(i),1024,LeaseCategory.Control);ba.Allocate(new(i),1024,LeaseCategory.Control);}
            Require(ab.Bytes==524288&&ba.Bytes==524288);
            Reject(()=>ab.Allocate(new(28),94,LeaseCategory.Control));
        });
        Check(RevisionTwoCase.SameAllocationReturn,()=>
        {
            var before=ab.Bytes+ba.Bytes;ab.BeginReturn(new(20));ba.BeginReturn(new(20));
            Require(ab.Bytes+ba.Bytes==before);
            ab.CompleteReturn(new(20),1024);ba.CompleteReturn(new(20),1024);
            Require(ab.Bytes+ba.Bytes==before&&ab.InFlight==15&&ba.InFlight==15);
        });
        Check(RevisionTwoCase.PendingResponseProgress,()=>
        {
            ab.Reuse(new(20),new(30));ba.Reuse(new(20),new(30));
            Require(ab.InFlight==16&&ba.InFlight==16&&ab.Bytes+ba.Bytes==1048576);
        });
        Check(RevisionTwoCase.StaleReturn,()=>
        {var before=ab.InFlight;Reject(()=>ab.CompleteReturn(new(20),1024));Require(ab.InFlight==before);});
        Check(RevisionTwoCase.MaximumControlShapes,()=>Require(80+4+8*(42+8)==484&&
            80+22+512==614&&80+26==106&&80+97==177&&614<=1024));
        Check(RevisionTwoCase.ReturnReceiptsFit,()=>
        {
            for(var n=1;n<=32;n++)Require(96+18*n<=80+4+30*n&&96+18*n<=80+4+42*n);
            Require(94<=96);
        });
        RequestIdentity Id(long generation,ulong sequence)=>new(new(generation),new(sequence));
        var oldFirst=Id(11,1);var oldSecond=Id(11,2);var reset=Id(11,3);var fresh=Id(12,1);
        var barrier=new Window(136);
        Check(RevisionTwoCase.BarrierIdentity,()=>
        {
            foreach(var id in new[]{oldFirst,oldSecond,reset,fresh}){Require(barrier.Admit(id));barrier.Commit(id);}
            Require(barrier.Count==4&&!barrier.Admit(oldFirst)&&!barrier.Admit(fresh));
        });
        Check(RevisionTwoCase.BarrierAcknowledgements,()=>
        {barrier.Acknowledge(new(11),new(3));Require(barrier.Count==1);barrier.Acknowledge(new(12),new(1));Require(barrier.Count==0);});
        Check(RevisionTwoCase.OriginalGenerationZero,()=>Reject(()=>barrier.Admit(Id(0,1))));
        Check(RevisionTwoCase.QueryWindow,()=>
        {
            var queries=new Window(64);
            for(ulong i=1;i<=64;i++)queries.Admit(Id(11,i));
            Reject(()=>queries.Admit(Id(11,65)));
            for(ulong i=1;i<=64;i++)queries.Commit(Id(11,i));
            Require(queries.Count==64);queries.Acknowledge(new(11),new(32));Require(queries.Count==32);
        });
        Check(RevisionTwoCase.AnimationRequestOwner,()=>Reject(()=>ContractFixture.Validate(
            new(1,MessageKind.AnimationCommands,RuntimeRole.Simulation,RuntimeRole.Animation,200,0,11,1))));
        var animation=new Window(128);var registrations=0;
        Check(RevisionTwoCase.AnimationDefinitionDuplicate,()=>
        {if(animation.Admit(Id(7,1)))registrations++;Require(!animation.Admit(Id(7,1))&&registrations==1);animation.Commit(Id(7,1));});
        Check(RevisionTwoCase.AnimationResultRelease,()=>
        {animation.Admit(Id(7,2));animation.Commit(Id(7,2));animation.Acknowledge(new(7),new(2));Require(animation.Count==0&&registrations==1);});
        Check(RevisionTwoCase.Animation2516,()=>{ReserveOutputs(2515,1);Require(ContractFixture.AnimationBytes(2516)==65533);});
        Check(RevisionTwoCase.Animation2517,()=>{Reject(()=>ReserveOutputs(2516,1));Require(ContractFixture.AnimationBytes(2517)==65559);});
        Check(RevisionTwoCase.Animation4096,()=>{Reject(()=>ReserveOutputs(0,4096));Require(ContractFixture.AnimationBytes(4096)==106613);});
        Check(RevisionTwoCase.HiddenOutputReservation,()=>Reject(()=>ReserveOutputs(2516,3)));
        Check(RevisionTwoCase.PhysicalVelocityBudget,()=>Require(ContractFixture.PhysicalBytes(257,256,256,0,0,0,257)==44057));
        Check(RevisionTwoCase.ResultWidths,()=>Require(8+8+8+2+4+8+2+8==48&&48+2+4==54&&54+8==62));
        Check(RevisionTwoCase.QueryWidths,()=>Require(8+8+2+8+4==30&&8+8+8+2+2+4==32));
        Check(RevisionTwoCase.DescriptorWidths,()=>Require(2+2+2+96+32+1+1+4==140&&2+8+56+2+2+4==74));
        Check(RevisionTwoCase.RangePositive,()=>ScalarRange(0,1,2,-2));
        Check(RevisionTwoCase.RangeReversed,()=>Reject(()=>ScalarRange(1,0)));
        Check(RevisionTwoCase.RangeEqual,()=>Reject(()=>ScalarRange(2,2)));
        Check(RevisionTwoCase.RangeOverflow,()=>Reject(()=>ScalarRange(-1e308,1e308)));
        Check(RevisionTwoCase.AngleOverflow,()=>Reject(()=>ScalarRange(0,1,-1e308,1e308)));
        var first=new ActiveBinding(new(7),new(1),new(50),Property.PropagationDistance,new(100),new(4));
        var sample=new Sample(new(7),new(1),new(50),Property.PropagationDistance,new(5),.5);
        var replacement=first with{Instance=new(2),Descriptor=new(101)};
        Check(RevisionTwoCase.SampleActive,()=>Require(CanApply(sample,first)));
        Check(RevisionTwoCase.SampleRetiredReuse,()=>Require(!CanApply(sample,replacement)&&
            CanApply(sample with{Instance=new(2)},replacement)));
        Check(RevisionTwoCase.SamplePending,()=>Require(!CanApply(sample,null)));
        Check(RevisionTwoCase.SampleFailed,()=>Require(!CanApply(sample with{Instance=new(2)},first)&&CanApply(sample,first)));
        Check(RevisionTwoCase.SampleDuplicateRemove,()=>
        {
            ActiveBinding? current=replacement;
            void Remove(InstanceIdentity instance){if(current?.Instance==instance)current=null;}
            Remove(new(1));Remove(new(1));Require(current==replacement);
        });
        Check(RevisionTwoCase.SampleReset,()=>Require(!CanApply(sample,first with{Generation=new(8)})));
        Check(RevisionTwoCase.SampleReordered,()=>Require(!CanApply(sample,first with{LastPublication=new(6)})));
        Check(RevisionTwoCase.PresentedOldOccurrence,()=>
        {
            bool Presented(InstanceIdentity instance,Generation generation,CommandSequence occurrence)=>
                instance==replacement.Instance&&generation==new Generation(11)&&occurrence==new CommandSequence(20);
            Require(!Presented(new(1),new(11),new(20))&&!Presented(new(2),new(11),new(19))&&
                Presented(new(2),new(11),new(20)));
        });
        var read=new QueryStamp(Id(11,1),new(40),QueryOutcome.Found);
        static bool Current(QueryStamp value,Generation generation,Revision revision)=>
            value.Identity.Generation==generation&&value.Required==revision&&value.Outcome==QueryOutcome.Found;
        Check(RevisionTwoCase.QueryDelayedRevision,()=>Require(!Current(read,new(11),new(41))&&
            Current(read,new(11),new(40))&&read.Required.Value==40));
        Check(RevisionTwoCase.QueryResetStamp,()=>Require(!Current(read,new(12),new(40))));
        Check(RevisionTwoCase.QueryCancelledStamp,()=>
        {
            var cancelled=read with{Outcome=QueryOutcome.Cancelled};
            Require(cancelled.Required==read.Required&&cancelled.Identity==read.Identity&&
                !Current(cancelled,new(11),new(40)));
        });
        Check(RevisionTwoCase.QueryDuplicateRelease,()=>
        {
            var queries=new Window(64);queries.Admit(read.Identity);queries.Commit(read.Identity);
            Require(!queries.Admit(read.Identity));queries.Admit(Id(12,1));queries.Commit(Id(12,1));
            queries.Acknowledge(new(11),new(1));queries.Acknowledge(new(11),new(1));
            Require(queries.Count==1);queries.Acknowledge(new(12),new(1));Require(queries.Count==0);
        });
        Check(RevisionTwoCase.QueryRejected,()=>
        {
            var queries=new Window(1);queries.Admit(Id(11,1));Reject(()=>queries.Admit(Id(11,2)));
            queries.Commit(Id(11,1));queries.Acknowledge(new(11),new(1));Require(queries.Count==0);
        });
        Check(RevisionTwoCase.QueryBarrierCancelled,()=>
        {
            var terminal=ResolveQuery(read,false,QueryTransition.Barrier);
            Require(terminal.Outcome==QueryOutcome.Cancelled&&terminal.Identity==read.Identity&&
                terminal.Required==read.Required&&ResolveQuery(terminal,true,QueryTransition.Barrier)==terminal);
        });
        Check(RevisionTwoCase.QueryRevisionStale,()=>
        {
            var terminal=ResolveQuery(read,false,QueryTransition.RevisionUnavailable);
            Require(terminal.Outcome==QueryOutcome.Stale&&terminal.Required==read.Required&&
                ResolveQuery(terminal,true,QueryTransition.Barrier)==terminal);
        });
        Check(RevisionTwoCase.QueryCompletedBarrier,()=>Require(
            ResolveQuery(read,true,QueryTransition.Barrier)==read));
        static bool Near(double a,double b)=>Math.Abs(a-b)<1e-12;
        Check(RevisionTwoCase.WaveEarlyReceipt,()=>Require(
            Near(Wave(.1,false,.1).Distance,1.2)&&Near(Wave(.1,true,.1).Distance,1.2)&&
            !Wave(.1,true,.1).Complete));
        Check(RevisionTwoCase.WaveEarlyContinues,()=>Require(
            Near(Wave(.2,true,.1).Distance,2.4)&&Near(Wave(.65,true,.1).Distance,7.8)&&
            !Wave(.65,true,.1).Complete));
        Check(RevisionTwoCase.WaveRangeBoundary,()=>Require(
            !Wave(8.0/12-1e-6,true,.1).Complete&&Wave(8.0/12,true,.1)==(8,true)));
        Check(RevisionTwoCase.WaveLateHeld,()=>Require(Wave(.7,false,.1)==(.1,false)&&
            Wave(.7,true,.1)==(8,true)));
        Check(RevisionTwoCase.WaveDelayedReceipt,()=>Require(
            !Wave(.65,true,.1).Complete&&Wave(.7,true,.1).Complete));
        Check(RevisionTwoCase.WaveLostReceipt,()=>Require(Wave(.7,false,.1)==(.1,false)&&
            Wave(10,false,.1)==(.1,false)&&Wave(10,true,.1)==(8,true)));
        return results;
    }
}
