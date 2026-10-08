using System.Text.Json;
using CuriousContraptions.LifecycleDesign;

enum Oracle
{
    ModeMatrix=1, IntegrityLost=2, SchemaPositive=3, MissingField=4, UnknownField=5,
    DuplicateField=6, NullSchema=7, UnknownMode=8, UnknownCommand=9, UnknownIntegrity=10,
    FaultIntegrityMismatch=11, ZeroStep=12, TwoSteps=13, UnexpectedStep=14,
    ClockGolden=15, ClockDrift=16, NegativeRoundTrip=17, ExcessUncertainty=18,
    StaleClock=19, ClockBoundary=20, NegativeClock=21, ReversedClock=22, PrecisionLimit=23,
    HistoryPositive=24, HistorySmall=25, HistoryMaximumReject=26, HistoryBoundary=27,
    HistoryEnvelopeReject=28, FailureMatrix=29, UnknownFailure=30, UnknownMutation=31,
    ReusedSlot=32, RemovedIdentity=33, StaleGeneration=34, ZeroIdentity=35,
    PhysicalTickTime=36, PausedClock=37, Interpolation=38, AggregateBudget=39, SampleContainment=40, SampleUncertainty=41,
    QuantizationOverlap=42, DisjointProbe=43, TouchingProbe=44, HistoryThreeStreams=45,
    HistoryCounterexample=46, AggregateResultsFull=47, ResultGenerationLimit=48,
    RetirementAcknowledged=49, UiDuringRetirement=50, InvalidResultWindow=51,
    HistoryReplacementPeak=52, MutationBoundary=53
}
sealed record Result(Oracle Oracle,HostMode? Mode,CommandKind? Command,FailurePoint? Point,
    MutationFamily? Mutation,bool Passed,string Detail);
static class Program
{
    static readonly List<Result> Results=[];
    static void Check(Oracle oracle,Action action,HostMode? mode=null,CommandKind? command=null,
        FailurePoint? point=null,MutationFamily? mutation=null)
    {
        try {action();Results.Add(new(oracle,mode,command,point,mutation,true,"Passed"));}
        catch(Exception error){Results.Add(new(oracle,mode,command,point,mutation,false,error.Message));}
    }
    static void Require(bool condition)
    {if(!condition)throw new InvalidOperationException("Oracle mismatch.");}
    static void Reject(Action action)
    {
        try{action();}
        catch(Exception error)when(error is ArgumentException or JsonException or OverflowException){return;}
        throw new InvalidOperationException("Invalid fixture accepted.");
    }
    static int Main()
    {
        // Independently enumerated accepted sets; all other pairs are InvalidMode.
        Dictionary<HostMode,CommandKind[]> applicable=new()
        {
            [HostMode.Building]=[CommandKind.ReplaceConstruction,CommandKind.CreateEntity,
                CommandKind.ReplaceEntity,CommandKind.RemoveEntity,CommandKind.Connect,
                CommandKind.Disconnect,CommandKind.Run,CommandKind.Reset,CommandKind.Save,CommandKind.Cancel],
            [HostMode.Running]=[CommandKind.ReplaceConstruction,CommandKind.BinaryInput,CommandKind.ScalarInput,
                CommandKind.Pulse,CommandKind.Pause,CommandKind.Reset,CommandKind.Cancel],
            [HostMode.Paused]=[CommandKind.ReplaceConstruction,CommandKind.Resume,CommandKind.Step,
                CommandKind.Reset,CommandKind.Cancel],
            [HostMode.Completed]=[CommandKind.ReplaceConstruction,CommandKind.Reset,CommandKind.Cancel],
            [HostMode.Faulted]=[CommandKind.ReplaceConstruction,CommandKind.Reset,CommandKind.Cancel]
        };
        foreach(var mode in Enum.GetValues<HostMode>())
        foreach(var command in Enum.GetValues<CommandKind>())
        {
            var input=new Schema(mode,mode==HostMode.Faulted?Integrity.ReversibleFailure:Integrity.Intact,
                command,command==CommandKind.Step?1u:0u);
            Check(Oracle.ModeMatrix,()=>
            {
                var expected=applicable[mode].Contains(command)?Disposition.Apply:Disposition.InvalidMode;
                if(mode==HostMode.Paused&&command is CommandKind.BinaryInput or CommandKind.ScalarInput or CommandKind.Pulse)
                    expected=Disposition.WaitForTick;
                var decision=ContractFixture.Decide(input);
                Require(decision.Disposition==expected);
                var target=mode;
                if(expected==Disposition.Apply)
                {
                    if(command is CommandKind.ReplaceConstruction or CommandKind.Reset)target=HostMode.Building;
                    if(command is CommandKind.Run or CommandKind.Resume)target=HostMode.Running;
                    if(command==CommandKind.Pause)target=HostMode.Paused;
                }
                Require(decision.Mode==target);
            },mode,command);
            if(mode==HostMode.Faulted)Check(Oracle.IntegrityLost,()=>Require(
                ContractFixture.Decide(input with{Integrity=Integrity.IntegrityLost}).Disposition==
                Disposition.InvalidMode),mode,command);
        }
        var schema=new Schema(HostMode.Paused,Integrity.Intact,CommandKind.Step,1);
        var json=JsonSerializer.Serialize(schema);
        Check(Oracle.SchemaPositive,()=>Require(ContractFixture.Read(json)==schema));
        // Deliberately invalid raw external JSON, never domain selectors.
        Check(Oracle.MissingField,()=>Reject(()=>ContractFixture.Read("{}")));
        Check(Oracle.UnknownField,()=>Reject(()=>ContractFixture.Read(json[..^1]+",\"Extra\":1}")));
        Check(Oracle.DuplicateField,()=>Reject(()=>ContractFixture.Read(json[..^1]+",\"Mode\":3}")));
        Check(Oracle.NullSchema,()=>Reject(()=>ContractFixture.Read("null")));
        void Invalid(Oracle oracle,Schema value)=>Check(oracle,()=>Reject(()=>ContractFixture.Read(JsonSerializer.Serialize(value))));
        Invalid(Oracle.UnknownMode,schema with{Mode=(HostMode)0});
        Invalid(Oracle.UnknownCommand,schema with{Command=(CommandKind)17});
        Invalid(Oracle.UnknownIntegrity,schema with{Integrity=(Integrity)4});
        Invalid(Oracle.FaultIntegrityMismatch,schema with{Integrity=Integrity.ReversibleFailure});
        Invalid(Oracle.FaultIntegrityMismatch,schema with{Mode=HostMode.Faulted});
        Invalid(Oracle.ZeroStep,schema with{StepTicks=0});
        Invalid(Oracle.TwoSteps,schema with{StepTicks=2});
        Invalid(Oracle.UnexpectedStep,schema with{Command=CommandKind.Run});
        Check(Oracle.ClockGolden,()=>Require(ContractFixture.Calibrate(100,150.2m,150.3m,100.7m,.01m,0)==new ClockInterval(49.58m,50.22m)));
        Check(Oracle.ClockDrift,()=>Require(ContractFixture.Calibrate(100,150.2m,150.3m,100.7m,.01m,250)==new ClockInterval(49.455m,50.345m)));
        Check(Oracle.NegativeRoundTrip,()=>Reject(()=>ContractFixture.Calibrate(100,150.2m,150.3m,100.05m,.01m,0)));
        Check(Oracle.ExcessUncertainty,()=>Reject(()=>ContractFixture.Calibrate(100,150,150,102.002m,0,0)));
        Check(Oracle.StaleClock,()=>Reject(()=>ContractFixture.Calibrate(100,150,150,100,0,500.001m)));
        Check(Oracle.ClockBoundary,()=>Require(ContractFixture.Calibrate(100,150,150,101.5m,0,500).Uncertainty==1));
        Check(Oracle.NegativeClock,()=>Reject(()=>ContractFixture.Calibrate(-1,1,1,1,0,0)));
        Check(Oracle.ReversedClock,()=>Reject(()=>ContractFixture.Calibrate(100,150.3m,150.2m,101,0,0)));
        Check(Oracle.PrecisionLimit,()=>Reject(()=>ContractFixture.Calibrate(100,150,150,101,.101m,0)));
        Check(Oracle.HistoryPositive,()=>Require(ContractFixture.HistoryBytes(44057,6773,0)==153514));
        Check(Oracle.HistorySmall,()=>Require(ContractFixture.HistoryBytes(23497,6773,0)==91834));
        Check(Oracle.HistoryMaximumReject,()=>Reject(()=>ContractFixture.HistoryBytes(65536,65536,0)));
        Check(Oracle.HistoryBoundary,()=>Require(ContractFixture.HistoryBytes(32597,32597,0)==196606));
        Check(Oracle.HistoryBoundary,()=>Reject(()=>ContractFixture.HistoryBytes(32597,32598,0)));
        Check(Oracle.HistoryEnvelopeReject,()=>Reject(()=>ContractFixture.HistoryBytes(65537,80,0)));
        foreach(var point in Enum.GetValues<FailurePoint>())
        foreach(var mutation in Enum.GetValues<MutationFamily>())
        {
            Check(Oracle.FailureMatrix,()=>
            {
                var expected=point switch
                {
                    FailurePoint.AfterCommit=>new FailureExpectation(AuthorityResult.Committed,UiResult.Pending),
                    FailurePoint.Recipient=>new FailureExpectation(AuthorityResult.Committed,UiResult.Indeterminate),
                    FailurePoint.Restore=>new FailureExpectation(AuthorityResult.IntegrityLost,UiResult.Indeterminate),
                    FailurePoint.ProcessDeath=>new FailureExpectation(AuthorityResult.Unknown,UiResult.Indeterminate),
                    FailurePoint.Admitted=>new FailureExpectation(AuthorityResult.Unchanged,UiResult.Pending),
                    _=>new FailureExpectation(AuthorityResult.Unchanged,UiResult.Unchanged)
                };
                Require(ContractFixture.Expected(point,mutation)==expected);
            },point:point,mutation:mutation);
        }
        Check(Oracle.UnknownFailure,()=>Reject(()=>ContractFixture.Expected((FailurePoint)13,MutationFamily.Construction)));
        Check(Oracle.UnknownMutation,()=>Reject(()=>ContractFixture.Expected(FailurePoint.Mutation,(MutationFamily)29)));
        Check(Oracle.MutationBoundary,()=>Reject(()=>ContractFixture.Expected(FailurePoint.Mutation,(MutationFamily)0)));
        Check(Oracle.SampleContainment,()=>
        {
            var offset=ContractFixture.Calibrate(100,150.2m,150.3m,100.7m,.01m,0);
            var mapped=ContractFixture.MapSample(offset,175,.01m);
            Require(mapped==new ClockInterval(124.77m,125.43m)&&mapped.Lower<=125.41m&&mapped.Upper>=125.41m);
        });
        Check(Oracle.SampleUncertainty,()=>
        {
            var offset=ContractFixture.Calibrate(100,150.2m,150.3m,100.7m,.01m,250);
            Require(ContractFixture.MapSample(offset,175,.01m)==new ClockInterval(124.645m,125.555m));
        });
        Check(Oracle.SampleUncertainty,()=>Reject(()=>ContractFixture.MapSample(new(49,51),175,.001m)));
        Check(Oracle.SampleUncertainty,()=>Require(ContractFixture.MapSample(new(49,51),175,0).Uncertainty==1));
        var oldProbe=ContractFixture.Calibrate(100,150,150,100,.01m,0);
        Check(Oracle.QuantizationOverlap,()=>Require(ContractFixture.Consistent(oldProbe,
            ContractFixture.Calibrate(100,150.03m,150.03m,100,.01m,0))));
        Check(Oracle.DisjointProbe,()=>Require(!ContractFixture.Consistent(oldProbe,
            ContractFixture.Calibrate(100,150.05m,150.05m,100,.01m,0))));
        Check(Oracle.TouchingProbe,()=>Require(ContractFixture.Consistent(oldProbe,
            ContractFixture.Calibrate(100,150.04m,150.04m,100,.01m,0))));
        Check(Oracle.HistoryThreeStreams,()=>Require(ContractFixture.HistoryBytes(44057,6773,6773)==173833));
        Check(Oracle.HistoryThreeStreams,()=>Require(ContractFixture.HistoryBytes(23497,6773,6773)==112153));
        Check(Oracle.HistoryCounterexample,()=>Reject(()=>ContractFixture.HistoryBytes(44057,6773,22009)));
        Check(Oracle.HistoryReplacementPeak,()=>Require(
            ContractFixture.HistoryBytes(Math.Max(44057,23497),6773,6773)==173833));
        Check(Oracle.HistoryReplacementPeak,()=>Reject(()=>ContractFixture.HistoryBytes(44057+23497,6773+6773,6773)));
        Check(Oracle.AggregateResultsFull,()=>Require(!ContractFixture.CanAdmit(
            [new(new(7),64),new(new(8),64)],new(7))));
        Check(Oracle.ResultGenerationLimit,()=>Require(!ContractFixture.CanAdmit(
            [new(new(7),1),new(new(8),1)],new(9))));
        Check(Oracle.UiDuringRetirement,()=>Require(ContractFixture.CanAdmit(
            [new(new(7),63),new(new(8),64)],new(7))));
        Check(Oracle.RetirementAcknowledged,()=>Require(ContractFixture.CanAdmit(
            [new(new(7),64)],new(9))));
        Check(Oracle.ResultGenerationLimit,()=>Require(!ContractFixture.CanAdmit(
            [new(new(7),1),new(new(9),1)],new(10))));
        Check(Oracle.InvalidResultWindow,()=>Reject(()=>ContractFixture.CanAdmit(
            [new(new(7),1),new(new(8),1),new(new(9),1)],new(10))));
        Check(Oracle.InvalidResultWindow,()=>Reject(()=>ContractFixture.CanAdmit(
            [new(new(7),129)],new(8))));
        Binding[] bindings=[new(new(12),new(8001),new(0)),new(new(12),new(7001),new(1))];
        Check(Oracle.ReusedSlot,()=>Require(ContractFixture.Resolve(bindings,new(12),new(12),new(8001))==new Slot(0)));
        Check(Oracle.RemovedIdentity,()=>Reject(()=>ContractFixture.Resolve(bindings,new(12),new(12),new(1001))));
        Check(Oracle.StaleGeneration,()=>Reject(()=>ContractFixture.Resolve(bindings,new(12),new(11),new(8001))));
        Check(Oracle.ZeroIdentity,()=>Reject(()=>ContractFixture.Resolve(bindings,new(12),new(12),new(0))));
        Check(Oracle.PhysicalTickTime,()=>Require(3600m/120==30&&1m/120/4==1m/480));
        Check(Oracle.PausedClock,()=>Require(21m/120==.175m));
        Check(Oracle.Interpolation,()=>Require((1m/120+2m/120)/2==.0125m));
        Check(Oracle.AggregateBudget,()=>Require(6*512*1024+3*1024*1024+1024*1024+768*1024+256*1024==8*1024*1024));
        var runtimeClockResults = RuntimeClockControls.Run();
        var failures=Results.Count(result=>!result.Passed) + runtimeClockResults.Count(result => !result.Passed);
        Console.WriteLine(JsonSerializer.Serialize(new{DesignOnly=false,RuntimeQualified=false,
            Count=Results.Count + runtimeClockResults.Count,Failures=failures,Results,RuntimeClockResults=runtimeClockResults}));
        return failures==0?0:1;
    }
}
