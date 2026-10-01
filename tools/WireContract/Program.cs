using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.WireDesign;

enum Oracle
{
    HeaderPositive, UnknownVersion, UnknownMessage, UnknownSender, UnknownRecipient,
    SameContext, WrongRoute, HeaderTruncated, EnvelopeTooLarge, UnknownFlags,
    NegativeGeneration, ZeroSequence, MissingField, UnknownField, DuplicateField, NullHeader,
    UnsignedGolden, SignedGolden, IntegerBoundaries, IntegerOverflow, PhysicalBudget,
    AnimationBudget, AggregateBudget, EnvelopeOverflow, StableReordering, RemovedSlot,
    ReusedSlot, StaleGeneration, DuplicateIdentity, DuplicateStorage, ZeroIdentity,
    NextBoundary, ExactTick, LateTick, FutureBoundary, TooFarAhead, NonfiniteValue,
    UnknownSchedule, UnscheduledTick, LifecycleScheduled, WrongGeneration, SameTickOrder,
    FutureDoesNotBlock, HeaderWidth, ConstructionWidth, EntityWidth, TopologyBodyWidth,
    AnimationDefinitionWidth, DiagnosticWidths, ReservationAccounting
}
sealed record Result(Oracle Oracle,bool Passed,string Detail);
static class Program
{
    static readonly List<Result> Results=[];
    static void Check(Oracle oracle,Action action)
    {
        try { action();Results.Add(new(oracle,true,"Passed")); }
        catch(Exception error){Results.Add(new(oracle,false,error.Message));}
    }
    static void Require(bool predicate)
    {if(!predicate)throw new InvalidOperationException("Oracle mismatch.");}
    static void Reject(Action action)
    {
        try{action();}
        catch(Exception error) when(error is ArgumentException or JsonException or OverflowException){return;}
        throw new InvalidOperationException("Invalid fixture was accepted.");
    }
    static int Main()
    {
        var header=new HeaderFixture(1,MessageKind.Commands,RuntimeRole.Browser,RuntimeRole.Simulation,126,0,11,1);
        void Invalid(Oracle oracle,HeaderFixture value)=>Check(oracle,()=>Reject(()=>ContractFixture.Validate(value)));
        Check(Oracle.HeaderPositive,()=>Require(ContractFixture.ReadHeader(JsonSerializer.Serialize(header))==header));
        Invalid(Oracle.UnknownVersion,header with{Version=2});
        Invalid(Oracle.UnknownMessage,header with{Kind=(MessageKind)65535});
        Invalid(Oracle.UnknownSender,header with{Sender=(RuntimeRole)0});
        Invalid(Oracle.UnknownRecipient,header with{Recipient=(RuntimeRole)255});
        Invalid(Oracle.SameContext,header with{Recipient=RuntimeRole.Browser});
        Invalid(Oracle.WrongRoute,header with{Recipient=RuntimeRole.Animation});
        Invalid(Oracle.HeaderTruncated,header with{TotalBytes=79});
        Invalid(Oracle.EnvelopeTooLarge,header with{TotalBytes=65537});
        Invalid(Oracle.UnknownFlags,header with{Flags=(LeaseLeg)99});
        Invalid(Oracle.NegativeGeneration,header with{Generation=-1});
        Invalid(Oracle.ZeroSequence,header with{Sequence=0});
        // Raw JSON is confined to this deliberately malformed external schema boundary.
        Check(Oracle.MissingField,()=>Reject(()=>ContractFixture.ReadHeader("{}")));
        var validJson=JsonSerializer.Serialize(header);
        Check(Oracle.UnknownField,()=>Reject(()=>ContractFixture.ReadHeader(validJson[..^1]+",\"Unsupported\":1}")));
        Check(Oracle.DuplicateField,()=>Reject(()=>ContractFixture.ReadHeader(validJson[..^1]+",\"Version\":1}")));
        Check(Oracle.NullHeader,()=>Reject(()=>ContractFixture.ReadHeader("null")));
        Check(Oracle.UnsignedGolden,()=>Require(ContractFixture.UnsignedBytes(9007199254740993UL)
            .SequenceEqual(new byte[]{1,0,0,0,0,0,32,0})));
        Check(Oracle.SignedGolden,()=>Require(ContractFixture.SignedBytes(long.MinValue)
            .SequenceEqual(new byte[]{0,0,0,0,0,0,0,128})));
        Check(Oracle.IntegerBoundaries,()=>
        {
            foreach(var value in new ulong[]{0,1,9007199254740991,9007199254740992,9007199254740993,
                long.MaxValue,9223372036854775808,ulong.MaxValue})
                Require(BinaryPrimitives.ReadUInt64LittleEndian(ContractFixture.UnsignedBytes(value))==value);
            foreach(var value in new[]{long.MinValue,-9007199254740993,-1,0,1,9007199254740993,long.MaxValue})
                Require(BinaryPrimitives.ReadInt64LittleEndian(ContractFixture.SignedBytes(value))==value);
        });
        Check(Oracle.IntegerOverflow,()=>Reject(()=>_ = ulong.Parse("18446744073709551616")));
        Check(Oracle.PhysicalBudget,()=>Require(ContractFixture.PhysicalBytes(257,256,256,0,0,0)==23497));
        Check(Oracle.AnimationBudget,()=>Require(ContractFixture.AnimationBytes(256)==6773));
        Check(Oracle.AggregateBudget,()=>Require(6L*ContractFixture.MaximumEdgeBytes+
            3L*ContractFixture.MaximumTransferBytes+2L*1024*1024==ContractFixture.AggregateBytes));
        Check(Oracle.EnvelopeOverflow,()=>Reject(()=>ContractFixture.RequireEnvelopeSize(
            ContractFixture.PhysicalBytes(uint.MaxValue,0,0,0,0,0))));
        Binding[] original=[new(new(1001),new(0)),new(new(7001),new(1))];
        Binding[] reordered=[new(new(7001),new(3)),new(new(1001),new(9))];
        Binding[] reused=[new(new(8001),new(0)),new(new(7001),new(1))];
        Check(Oracle.StableReordering,()=>Require(ContractFixture.Canonical(original)
            .SequenceEqual(ContractFixture.Canonical(reordered))));
        Check(Oracle.RemovedSlot,()=>Reject(()=>ContractFixture.Resolve(reused,new(11),new(11),new(1001))));
        Check(Oracle.ReusedSlot,()=>Require(ContractFixture.Resolve(reused,new(11),new(11),new(8001))==new StorageSlot(0)));
        Check(Oracle.StaleGeneration,()=>Reject(()=>ContractFixture.Resolve(reused,new(12),new(11),new(7001))));
        Check(Oracle.DuplicateIdentity,()=>Reject(()=>ContractFixture.Canonical([original[0],original[0] with{Slot=new(2)}])));
        Check(Oracle.DuplicateStorage,()=>Reject(()=>ContractFixture.Canonical([original[0],original[1] with{Slot=new(0)}])));
        Check(Oracle.ZeroIdentity,()=>Reject(()=>ContractFixture.Canonical([new(new(0),new(0))])));
        var command=new FixtureCommand(new(1),new(11),ScheduleKind.ExactTick,new(43),CommandKind.ScalarInput,new(7001),2);
        void Schedule(Oracle oracle,FixtureCommand input,Admission expected)=>Check(oracle,()=>
            Require(ContractFixture.Schedule(input,new(11),new(40))==expected));
        Schedule(Oracle.NextBoundary,command with{Schedule=ScheduleKind.NextBoundary,Requested=new(0)},Admission.Accepted);
        Schedule(Oracle.ExactTick,command,Admission.Accepted);
        Schedule(Oracle.LateTick,command with{Requested=new(40)},Admission.TooLate);
        Schedule(Oracle.FutureBoundary,command with{Requested=new(1240)},Admission.Accepted);
        Schedule(Oracle.TooFarAhead,command with{Requested=new(1241)},Admission.TooFarAhead);
        Schedule(Oracle.NonfiniteValue,command with{Value=double.NaN},Admission.InvalidSchema);
        Schedule(Oracle.UnknownSchedule,command with{Schedule=(ScheduleKind)99},Admission.InvalidSchema);
        Schedule(Oracle.UnscheduledTick,command with{Schedule=ScheduleKind.NextBoundary},Admission.InvalidSchema);
        Schedule(Oracle.LifecycleScheduled,command with{Kind=CommandKind.Reset},Admission.InvalidSchema);
        Schedule(Oracle.WrongGeneration,command with{Generation=new(10)},Admission.WrongGeneration);
        Check(Oracle.SameTickOrder,()=>
        {
            var results=ContractFixture.Due([command with{Sequence=new(2)},command],new(43));
            Require(results.Length==2&&results[0].Sequence.Value==1&&results[0].Order==0&&results[1].Order==1);
        });
        Check(Oracle.FutureDoesNotBlock,()=>
        {
            var results=ContractFixture.Due([command,command with{Sequence=new(2),
                Schedule=ScheduleKind.NextBoundary,Requested=new(0)}],new(41));
            Require(results.Length==1&&results[0].Sequence.Value==2&&results[0].Tick.Value==41);
        });
        Check(Oracle.HeaderWidth,()=>Require(4+2+2+4+1+1+2+16+8+8+8+8+8+8==80));
        Check(Oracle.ConstructionWidth,()=>Require(2+16+32+8+8+8+1+8+8+4+4+4+4==107));
        Check(Oracle.EntityWidth,()=>Require(8+8+1+56+24+2+4+4+4==111));
        Check(Oracle.TopologyBodyWidth,()=>Require(8+8+2+2+8+8+8+96==140));
        Check(Oracle.AnimationDefinitionWidth,()=>Require(8+8+8+2+2+8+2+8+32+4==82));
        Check(Oracle.DiagnosticWidths,()=>Require(2+8+8+8+2+2==30&&30+4+8+8==50&&30+8==38));
        Check(Oracle.ReservationAccounting,()=>Require(ContractFixture.OrdinaryReservations==128&&
            ContractFixture.LifecycleReservations==8&&
            ContractFixture.MaximumEdgeEnvelopes-ContractFixture.ReservedControlEnvelopes==56&&128-32+32==128));
        var revisionTwo=RevisionTwoOracles.Run();
        Console.WriteLine(JsonSerializer.Serialize(new { DesignOnly=true,RuntimeQualified=false,
            Count=Results.Count+revisionTwo.Count,Failures=Results.Count(r=>!r.Passed)+revisionTwo.Count(r=>!r.Passed),Results,RevisionTwo=revisionTwo }));
        return Results.All(r=>r.Passed)&&revisionTwo.All(r=>r.Passed)?0:1;
    }
}
