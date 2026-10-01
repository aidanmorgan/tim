using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions.LifecycleDesign;

// Design-only compiler-checked choices. No production runtime references.
public enum HostMode { Building=1, Running=2, Paused=3, Completed=4, Faulted=5 }
public enum Integrity { Intact=1, ReversibleFailure=2, IntegrityLost=3 }
public enum CommandKind
{
    ReplaceConstruction=1, CreateEntity=2, ReplaceEntity=3, RemoveEntity=4, Connect=5,
    Disconnect=6, BinaryInput=7, ScalarInput=8, Pulse=9, Run=10, Pause=11,
    Resume=12, Step=13, Reset=14, Save=15, Cancel=16
}
public enum Disposition { Apply=1, WaitForTick=2, InvalidMode=3 }
public enum FailurePoint
{
    Schema=1, Admitted=2, Candidate=3, Capture=4, Mutation=5, Seal=6,
    BeforeCommit=7, AfterCommit=8, Recipient=9, Restore=10, ProcessDeath=11, Storage=12
}
public enum AuthorityResult { Unchanged=1, Committed=2, IntegrityLost=3, Unknown=4 }
public enum UiResult { Unchanged=1, Pending=2, Indeterminate=3 }
public enum MutationFamily
{
    Construction=1, WorldInstallation=2, AssemblyMaps=3, ColliderMetadata=4,
    JointDeclarations=5, LoadDeclarations=6, SurfaceDeclarations=7, ServoState=8,
    SensorState=9, EnergyGasSpring=10, PhysicsStep=11, SpatialCaches=12, Timers=13,
    Controllers=14, CapabilityMaps=15, PartRuntime=16, TickOutputs=17, CommandInbox=18,
    PosePublication=19, EventPublication=20, QueryTransferLedgers=21, AnimationKernel=22,
    BrowserBindings=23, AnimationOccurrences=24, AudioRecipient=25, PresentationHistory=26,
    BrowserConstruction=27, DiagnosticsBootstrap=28
}
public readonly record struct AnimationGeneration(ulong Value);
public readonly record struct ResultWindow(AnimationGeneration Generation,int Entries);
public readonly record struct Generation(long Value);
public readonly record struct StableId(ulong Value);
public readonly record struct Slot(int Value);
public readonly record struct Binding(Generation Generation, StableId Id, Slot Slot);
public readonly record struct Decision(Disposition Disposition, HostMode Mode);
public readonly record struct FailureExpectation(AuthorityResult Authority, UiResult Ui);
public sealed record Schema(
    [property:JsonRequired] HostMode Mode,
    [property:JsonRequired] Integrity Integrity,
    [property:JsonRequired] CommandKind Command,
    [property:JsonRequired] uint StepTicks);
public sealed record ClockInterval(decimal Lower,decimal Upper)
{
    public decimal Offset=>(Lower+Upper)/2;
    public decimal Uncertainty=>(Upper-Lower)/2;
}
public static class ContractFixture
{
    static readonly JsonSerializerOptions JsonOptions = new()
    { UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
    public static Schema Read(string json)
    {
        using var doc=JsonDocument.Parse(json);
        if(doc.RootElement.ValueKind!=JsonValueKind.Object) throw new JsonException("Object required.");
        var keys=new HashSet<string>(StringComparer.Ordinal); // External JSON field-name boundary only.
        foreach(var property in doc.RootElement.EnumerateObject())
            if(!keys.Add(property.Name)) throw new JsonException("Duplicate field.");
        var value=JsonSerializer.Deserialize<Schema>(json,JsonOptions)??throw new JsonException("Object required.");
        Validate(value);
        return value;
    }
    public static void Validate(Schema value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(!Enum.IsDefined(value.Mode)||!Enum.IsDefined(value.Integrity)||!Enum.IsDefined(value.Command))
            throw new ArgumentException("Unknown enum.");
        if((value.Mode==HostMode.Faulted)==(value.Integrity==Integrity.Intact))
            throw new ArgumentException("Fault mode and integrity disagree.");
        if(value.StepTicks!=(value.Command==CommandKind.Step?1u:0u))
            throw new ArgumentException("Only one whole Step tick is supported.");
    }
    public static Decision Decide(Schema value)
    {
        Validate(value);
        if(value.Integrity==Integrity.IntegrityLost)
            return new(Disposition.InvalidMode,value.Mode);
        return value.Command switch
        {
            CommandKind.ReplaceConstruction or CommandKind.Reset => new(Disposition.Apply,HostMode.Building),
            CommandKind.Cancel => new(Disposition.Apply,value.Mode),
            CommandKind.CreateEntity or CommandKind.ReplaceEntity or CommandKind.RemoveEntity or
                CommandKind.Connect or CommandKind.Disconnect or CommandKind.Save
                => value.Mode==HostMode.Building?new(Disposition.Apply,HostMode.Building):
                    new(Disposition.InvalidMode,value.Mode),
            CommandKind.BinaryInput or CommandKind.ScalarInput or CommandKind.Pulse
                => value.Mode switch
                {
                    HostMode.Running => new(Disposition.Apply,HostMode.Running),
                    HostMode.Paused => new(Disposition.WaitForTick,HostMode.Paused),
                    _ => new(Disposition.InvalidMode,value.Mode)
                },
            CommandKind.Run => value.Mode==HostMode.Building?new(Disposition.Apply,HostMode.Running):
                new(Disposition.InvalidMode,value.Mode),
            CommandKind.Pause => value.Mode==HostMode.Running?new(Disposition.Apply,HostMode.Paused):
                new(Disposition.InvalidMode,value.Mode),
            CommandKind.Resume => value.Mode==HostMode.Paused?new(Disposition.Apply,HostMode.Running):
                new(Disposition.InvalidMode,value.Mode),
            CommandKind.Step => value.Mode==HostMode.Paused?new(Disposition.Apply,HostMode.Paused):
                new(Disposition.InvalidMode,value.Mode),
            _ => throw new ArgumentException("Unknown command.")
        };
    }
    public static FailureExpectation Expected(FailurePoint point,MutationFamily mutation)
    {
        if(!Enum.IsDefined(point)||!Enum.IsDefined(mutation))
            throw new ArgumentException("Unknown failure or mutation identity.");
        return point switch
        {
            FailurePoint.AfterCommit => new(AuthorityResult.Committed,UiResult.Pending),
            FailurePoint.Recipient => new(AuthorityResult.Committed,UiResult.Indeterminate),
            FailurePoint.Restore => new(AuthorityResult.IntegrityLost,UiResult.Indeterminate),
            FailurePoint.ProcessDeath => new(AuthorityResult.Unknown,UiResult.Indeterminate),
            FailurePoint.Admitted => new(AuthorityResult.Unchanged,UiResult.Pending),
            _ => new(AuthorityResult.Unchanged,UiResult.Unchanged)
        };
    }
    // Milliseconds in this independent arithmetic oracle; wire timestamps are integer nanoseconds.
    public static ClockInterval Calibrate(decimal b0,decimal w1,decimal w2,decimal b3,
        decimal endpointPrecision,decimal age)
    {
        if(b0<0||w1<0||w2<w1||b3<b0||b3-b0<w2-w1||
            endpointPrecision<0||endpointPrecision>.1m||age<0||age>500)
            throw new ArgumentException("Invalid clock observation.");
        var low=w2-b3;var high=w1-b0;
        var uncertainty=(high-low)/2+2*endpointPrecision+age*.0005m;
        if(uncertainty>1)throw new ArgumentException("Mapping uncertainty exceeded.");
        var midpoint=(low+high)/2;
        return new(midpoint-uncertainty,midpoint+uncertainty);
    }
    public static ClockInterval MapSample(ClockInterval offset,decimal workerStamp,decimal samplePrecision)
    {
        if(offset.Lower>offset.Upper||workerStamp<0||samplePrecision<0||samplePrecision>.1m)
            throw new ArgumentException("Invalid sample stamp.");
        var mapped=new ClockInterval(workerStamp-samplePrecision-offset.Upper,
            workerStamp+samplePrecision-offset.Lower);
        if(mapped.Uncertainty>1)throw new ArgumentException("Sample mapping uncertainty exceeded.");
        return mapped;
    }
    public static bool Consistent(ClockInterval previous,ClockInterval current)
    {
        if(previous.Lower>previous.Upper||current.Lower>current.Upper)
            throw new ArgumentException("Invalid clock interval.");
        return previous.Lower<=current.Upper&&current.Lower<=previous.Upper;
    }
    public static long HistoryBytes(long physical,long worldAnimation,long uiAnimation)
    {
        if(physical is <0 or >65536||worldAnimation is <0 or >65536||uiAnimation is <0 or >65536||
            physical is >0 and <80||worldAnimation is >0 and <80||uiAnimation is >0 and <80)
            throw new ArgumentException("Invalid envelope size.");
        var bytes=checked(3*(physical+worldAnimation+uiAnimation)+1024);
        if(bytes>192*1024)throw new ArgumentException("Combined history capacity exceeded.");
        return bytes;
    }
    public static bool CanAdmit(ReadOnlySpan<ResultWindow> windows,AnimationGeneration requested)
    {
        if(requested.Value==0)throw new ArgumentException("Invalid animation generation.");
        var generations=new HashSet<AnimationGeneration>();
        var count=0;
        foreach(var window in windows)
        {
            if(window.Generation.Value==0||window.Entries<1||!generations.Add(window.Generation))
                throw new ArgumentException("Invalid result window.");
            count=checked(count+window.Entries);
        }
        if(count>128||generations.Count>2)throw new ArgumentException("Invalid aggregate retention.");
        return count<128&&(generations.Contains(requested)||generations.Count<2);
    }
    public static Slot Resolve(ReadOnlySpan<Binding> bindings,Generation installed,Generation requested,StableId id)
    {
        if(installed.Value<=0||requested!=installed||id.Value==0)throw new ArgumentException("Stale identity.");
        foreach(var binding in bindings)
            if(binding.Generation==installed&&binding.Id==id)return binding.Slot;
        throw new ArgumentException("Removed identity.");
    }
}
