using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

public enum EngineCapability
{
    ContactImpulse, SlidingFriction, JointConstraint, TensionTransmission, ShaftTorque,
    ElectricalPower, SignalPropagation, FluidAdvection, PressureWork, Buoyancy, AerodynamicDrag,
    AcousticPropagation, OpticalTransport, OpticalAbsorption, IonizingTransport, RadioactiveDecay,
    TemperatureSensing, ThermalConduction, ThermalConvection, ThermalRadiation, SolidMelting,
    LiquidFreezing, LiquidEvaporation, LiquidBoiling, VaporCondensation, SolidSublimation,
    VaporDeposition, ChemicalReaction, Ignition, Extinction, ThermalExpansion, ThermalStress,
    HeatPumping, ThermoelectricConversion, PhaseStorage, MaterialCoating, StructuralFracture,
    CapillaryTransport, ElectricalDissipation, GasState, PhaseTopology, TemperatureStrength,
    SensibleHeat, ElasticStorage, FiniteWorkActuation, FieldForce, GranularTransport,
    ProgrammableController, ObjectiveEvaluation, GeometryQuery, ConstructionCompiler,
    StateTransaction, FiniteLedger, DependencyScheduling, TimedCommand, ReliablePublication,
    WorkerTransport, SimulationClock, AnimationEvaluation, AnimationLifecycle,
    PresentationHistory, RenderApplication, ResourceLifetime, PerformanceMeasurement,
    InputAdmission, ConstructionPersistence, DifficultyAssistance, TypedContracts,
    RigidBodyDynamics, EnvironmentState, TopologyTransaction
}
public enum SourceRelationKind { CoverageScope, SourceReference }
public sealed record SourceRelation(SourceRelationKind Kind,SourceKey Target);
public enum ObligationKind { Engine, CurrentConsumer, FutureProduct, ProductWorkflow, ResearchContainer }
public enum ConsumerKind { CurrentPart, AuthoredFixture, FutureDeclaration, EngineService, ProductWorkflow }
public enum CapabilityAvailability { Partial, Missing }
public enum MeasurementUnit { Dimensionless, Metre, Second, Kilogram, Radian, Newton, NewtonMetre,
    Joule, Watt, Pascal, CubicMetre, Kelvin, Coulomb, Volt, Ampere, Mole, GameExposure, ParticleCount, Hertz, Byte }
public enum ModeDimension { Configuration, Logic, OpticalChannel, AcousticTone, TubeAngle, Lifecycle }
public enum ModeChoice { Fixed, And, Or, Xor, Nor, Nand, Broadband, Red, Green, Blue, Yellow, Cyan,
    Magenta, White, Low, Mid, High, Degrees45, Degrees90, Run, Pause, Step, Reset, Save, Load }

public readonly record struct WorkOrderId
{
    public string Value { get; }
    public WorkOrderId(string value)
    {
        if(value is null || !Regex.IsMatch(value,@"\A[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*\z"))
            throw new ArgumentException("Invalid work-order identity.");
        Value=value;
    }
}
public readonly record struct ConsumerId(RequirementId Identity);
public readonly record struct SourcePath
{
    public string Value { get; }
    public SourcePath(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||Path.IsPathRooted(value)||value.Split('/').Any(p=>p is "" or "." or "..")||value.Contains('\\'))
            throw new ArgumentException("Invalid repository source path.");
        Value=value;
    }
}
public readonly record struct SymbolId
{
    public string Value { get; }
    public SymbolId(string value)
    {
        if(value is null||!Regex.IsMatch(value,@"\A[A-Za-z_][A-Za-z0-9_.]*\z"))
            throw new ArgumentException("Invalid source symbol identity.");
        Value=value;
    }
}
public sealed record SourceArtifact(SourcePath Path,ContentHash Hash);
public sealed record SourceSymbol(SourcePath Path,SymbolId Symbol,ContentHash FileHash);
public sealed record CapabilityContract(EngineCapability Capability,string ModelBoundary,
    MeasurementUnit[] Units,WorkOrderId DesignOwner,WorkOrderId[] ImplementationOwners,
    WorkOrderId ProofOwner,CapabilityAvailability Availability,SourceSymbol[] CurrentSymbols,
    string IndependentOracle,SourceKey[] BoundSources,EngineCapability[] Dependencies);
public sealed record ConsumerContract(ConsumerId Id,ConsumerKind Kind,SourceKey Source,
    SourceSymbol[] Symbols,EngineCapability[] Capabilities,WorkOrderId ProofOwner,SourceArtifact[] Artifacts);
public sealed record ModeContract(SourceKey Source,ModeDimension Dimension,ModeChoice Choice,
    WorkOrderId ProofOwner);
public sealed record CapabilityBinding(SourceKey Source,ContentHash SourceHash,ObligationKind Kind,
    EngineCapability[] Capabilities,ConsumerId[] Consumers,SourceRelation[] Relations,
    WorkOrderId ProofOwner,string ScopeReason);
public sealed record CapabilityInventory(CapabilityContract[] Capabilities,ConsumerContract[] Consumers,
    CapabilityBinding[] Bindings,ModeContract[] Modes);
public sealed record CapabilityInventoryIndex(SourcePath[] Files);
public sealed record CapabilityOwners(WorkOrderId Design,WorkOrderId[] Implementation,WorkOrderId Proof);
public sealed record CapabilityExpectations(IReadOnlySet<WorkOrderId> Owners,
    IReadOnlyDictionary<SourceKey,WorkOrderId> SourceOwners,
    IReadOnlyDictionary<EngineCapability,CapabilityOwners> CapabilityOwners,
    IReadOnlyList<ModeContract> Modes,
    IReadOnlyDictionary<SourceKey,SourceRelation[]> Relations,
    IReadOnlyDictionary<EngineCapability,EngineCapability[]> RequiredDependencies,
    IReadOnlyDictionary<SourceKey,SourceArtifact[]> RequiredArtifacts,
    IReadOnlyDictionary<SourceKey,EngineCapability[]> RequiredCapabilities,
    IReadOnlyDictionary<SourceKey,SourceSymbol[]> RequiredSymbols,
    IReadOnlyDictionary<SourceKey,SourceClassification> Classifications,
    IReadOnlyDictionary<EngineCapability,RequiredDeclaration[]> ImplementationDeclarations,
    IReadOnlySet<WorkOrderId> ImplementationEligibleOwners);
public sealed record SourceClassification(ObligationKind Obligation,ConsumerKind Consumer);
public sealed record CapabilitySummary(int Sources,int Capabilities,int Consumers,int Modes,
    int Missing,int Orphaned,int Changed,bool InventoryCurrent,bool RuntimeQualified);

/// <summary>Checks scoped inventory currency and declared relations, never runtime qualification.
/// Source declarations are checked lexically against hashed files, not resolved by a compiler.</summary>
public static class CapabilityAudit
{
    public static CapabilitySummary Analyze(IReadOnlyList<SourceRequirement> sources,
        CapabilityInventory inventory,CapabilityExpectations expected,Func<SourcePath,string> readSource)
    {
        NotNull(inventory);NotNull(expected);NotNull(readSource);
        RequirementDiscovery.RequireUnique(sources);
        var known=sources.ToDictionary(x=>x.Key);
        var capabilities=new Dictionary<EngineCapability,CapabilityContract>();
        foreach(var capability in Required(inventory.Capabilities))
        {
            NotNull(capability);
            if(!Enum.IsDefined(capability.Capability)||!Enum.IsDefined(capability.Availability)||
                !capabilities.TryAdd(capability.Capability,capability))Fail("Invalid or duplicate capability.");
            Text(capability.ModelBoundary);Text(capability.IndependentOracle);
            var units=Required(capability.Units);Unique(units);
            if(units.Length==0||units.Any(x=>!Enum.IsDefined(x)))Fail("Missing or invalid units.");
            Owner(capability.DesignOwner);Owner(capability.ProofOwner);
            var implementationOwners=Required(capability.ImplementationOwners);Unique(implementationOwners);
            if(implementationOwners.Length==0)Fail("Capability has no implementation owner.");
            foreach(var implementationOwner in implementationOwners)
            {
                Owner(implementationOwner);
                if(!expected.ImplementationEligibleOwners.Contains(implementationOwner))
                    Fail("Capability implementation owner is not an executable implementation work order.");
            }
            if(!expected.CapabilityOwners.TryGetValue(capability.Capability,out var owner)||
                owner.Design!=capability.DesignOwner||owner.Proof!=capability.ProofOwner||
                !implementationOwners.ToHashSet().SetEquals(owner.Implementation))
                Fail("Capability has wrong design, implementation or proof owner.");
            Unique(Required(capability.BoundSources));Unique(Required(capability.Dependencies));
            if(!expected.RequiredDependencies.TryGetValue(capability.Capability,out var requiredDependencies)||
                !capability.Dependencies.ToHashSet().SetEquals(requiredDependencies))
                Fail("Capability dependencies differ from required law contract.");
            foreach(var child in capability.BoundSources)if(!known.ContainsKey(child))Fail("Unknown capability child.");
            CheckSymbols(capability.CurrentSymbols);
            var declarations=expected.ImplementationDeclarations.TryGetValue(capability.Capability,out var declared)?declared:[];
            if(!capability.CurrentSymbols.Select(symbol=>new RequiredDeclaration(symbol.Path,symbol.Symbol)).ToHashSet().SetEquals(declarations))
                Fail("Capability declaration set differs from current implementation contract.");
            if(capability.Availability==CapabilityAvailability.Partial&&capability.CurrentSymbols.Length==0)
                Fail("Partial capability requires its current implementation symbols.");
            if(capability.Availability==CapabilityAvailability.Missing&&capability.CurrentSymbols.Length!=0)
                Fail("Missing capability cannot claim implementation symbols.");
        }
        if(!Enum.GetValues<EngineCapability>().ToHashSet().SetEquals(capabilities.Keys))
            Fail("Missing required engine capability.");
        foreach(var capability in capabilities.Values)
        {
            foreach(var dependency in capability.Dependencies)
                if(dependency==capability.Capability||!capabilities.ContainsKey(dependency))Fail("Invalid capability dependency.");
            _=Closure(capability.Capability,[]);
        }
        var consumers=new Dictionary<ConsumerId,ConsumerContract>();
        var consumerSources=new HashSet<SourceKey>();
        foreach(var consumer in Required(inventory.Consumers))
        {
            NotNull(consumer);_=new RequirementId(consumer.Id.Identity.Value);
            if(!Enum.IsDefined(consumer.Kind)||!consumers.TryAdd(consumer.Id,consumer)||!known.ContainsKey(consumer.Source)||!consumerSources.Add(consumer.Source))
                Fail("Invalid, duplicate or orphan consumer.");
            if(!expected.Classifications.TryGetValue(consumer.Source,out var classification)||consumer.Kind!=classification.Consumer)
                Fail("Consumer kind differs from authoritative source classification.");
            SourceOwner(consumer.Source,consumer.ProofOwner);CheckCapabilities(consumer.Capabilities);CheckSymbols(consumer.Symbols);
            if(expected.RequiredSymbols.TryGetValue(consumer.Source,out var requiredSymbols)&&
                requiredSymbols.Any(symbol=>!consumer.Symbols.Contains(symbol)))Fail("Missing actual consumer declaration symbol.");
            var artifacts=Required(consumer.Artifacts);
            if(expected.RequiredArtifacts.TryGetValue(consumer.Source,out var requiredArtifacts)&&
                requiredArtifacts.Any(artifact=>!artifacts.Contains(artifact)))Fail("Missing required consumer artifact.");
            foreach(var artifact in artifacts)
            {
                NotNull(artifact);_=new SourcePath(artifact.Path.Value);
                if(RequirementDiscovery.Hash(readSource(artifact.Path))!=artifact.Hash)Fail("Stale consumer artifact.");
            }
            if(consumer.Kind==ConsumerKind.CurrentPart && consumer.Symbols.Length==0)
                Fail("Current part requires source symbols.");
        }
        var bindings=new Dictionary<SourceKey,CapabilityBinding>();var changed=0;
        foreach(var binding in Required(inventory.Bindings))
        {
            NotNull(binding);
            if(!Enum.IsDefined(binding.Kind)||!bindings.TryAdd(binding.Source,binding))Fail("Invalid or duplicate source binding.");
            _=new RequirementId(binding.Source.Id.Value);_=new ContentHash(binding.SourceHash.Value);
            if(!Enum.IsDefined(binding.Source.Origin))Fail("Invalid source origin.");
            if(known.ContainsKey(binding.Source))SourceOwner(binding.Source,binding.ProofOwner);
            else Owner(binding.ProofOwner);
            if(!expected.Classifications.TryGetValue(binding.Source,out var classification)||binding.Kind!=classification.Obligation)
                Fail("Obligation kind differs from authoritative source classification.");
            Text(binding.ScopeReason);CheckCapabilities(binding.Capabilities);
            if(expected.RequiredCapabilities.TryGetValue(binding.Source,out var requiredCapabilities)&&
                requiredCapabilities.Any(capability=>!binding.Capabilities.Contains(capability)))
                Fail("Missing required source capability.");
            Unique(Required(binding.Consumers));Unique(Required(binding.Relations));
            if(binding.Consumers.Length!=1)Fail("Source must have exactly one named consumer.");
            foreach(var id in binding.Consumers)
            {
                if(!consumers.TryGetValue(id,out var consumer)||consumer.Source!=binding.Source||
                    !consumer.Capabilities.ToHashSet().SetEquals(binding.Capabilities))
                    Fail("Consumer does not match the source and capability binding.");
            }
            foreach(var relation in binding.Relations)
            {
                NotNull(relation);
                if(!Enum.IsDefined(relation.Kind)||relation.Target==binding.Source||!known.ContainsKey(relation.Target))
                    Fail("Invalid source relation.");
            }
            var relations=expected.Relations.TryGetValue(binding.Source,out var requiredRelations)?requiredRelations:[];
            if(!binding.Relations.ToHashSet().SetEquals(relations))Fail($"Source relations differ from required traceability scope: {binding.Source.Id.Value}.");
            if(known.TryGetValue(binding.Source,out var source)&&source.Hash!=binding.SourceHash)changed++;
        }
        foreach(var binding in bindings.Values)
            foreach(var relation in binding.Relations)
                if(!bindings.ContainsKey(relation.Target))Fail("Missing related source binding.");
        foreach(var capability in capabilities.Values)
        {
            var required=bindings.Values.Where(binding=>binding.Capabilities.Contains(capability.Capability)).Select(binding=>binding.Source);
            if(!capability.BoundSources.ToHashSet().SetEquals(required))Fail("Capability child ledger differs from source bindings.");
        }
        if(consumers.Keys.Any(id=>!bindings.Values.Any(binding=>binding.Consumers.Contains(id))))Fail("Unreferenced consumer.");
        var modes=new HashSet<ModeContract>();
        foreach(var mode in Required(inventory.Modes))
        {
            NotNull(mode);
            if(!ValidMode(mode.Dimension,mode.Choice)||!modes.Add(mode)||!known.ContainsKey(mode.Source))
                Fail("Invalid, duplicate or orphan mode.");
            SourceOwner(mode.Source,mode.ProofOwner);
        }
        if(!modes.SetEquals(expected.Modes))Fail("Missing, unexpected or wrongly owned mode.");
        var missing=known.Keys.Count(x=>!bindings.ContainsKey(x));
        var orphaned=bindings.Keys.Count(x=>!known.ContainsKey(x));
        return new(known.Count,capabilities.Count,consumers.Count,modes.Count,missing,orphaned,changed,
            missing==0&&orphaned==0&&changed==0,false);

        void Owner(WorkOrderId id)
        {
            _=new WorkOrderId(id.Value);
            if(!expected.Owners.Contains(id))Fail("Unknown proof or implementation owner.");
        }
        void SourceOwner(SourceKey source,WorkOrderId id)
        {
            Owner(id);
            if(!expected.SourceOwners.TryGetValue(source,out var owner)||owner!=id)Fail("Wrong source proof owner.");
        }
        HashSet<EngineCapability> Closure(EngineCapability value,HashSet<EngineCapability> path)
        {
            if(!path.Add(value))throw new InvalidDataException("Cyclic capability dependency.");
            var result=new HashSet<EngineCapability>{value};
            foreach(var dependency in capabilities[value].Dependencies)result.UnionWith(Closure(dependency,new(path)));
            return result;
        }
        void CheckCapabilities(EngineCapability[] values)
        {
            Unique(Required(values));
            if(values.Length==0||values.Any(x=>!capabilities.ContainsKey(x)))Fail("Missing or unknown capability binding.");
            foreach(var value in values)
                if(!Closure(value,[]).IsSubsetOf(values.ToHashSet()))Fail("Incomplete capability dependency closure.");
        }
        void CheckSymbols(SourceSymbol[] symbols)
        {
            Unique(Required(symbols));
            foreach(var symbol in symbols)
            {
                NotNull(symbol);_=new SourcePath(symbol.Path.Value);_=new SymbolId(symbol.Symbol.Value);
                var text=readSource(symbol.Path);
                if(RequirementDiscovery.Hash(text)!=symbol.FileHash)Fail("Stale source symbol.");
                var declaration=@"\b(?:record(?:\s+(?:class|struct))?|class|struct|interface|enum)\s+"+
                    Regex.Escape(symbol.Symbol.Value)+@"\b";
                if(!Regex.IsMatch(text,declaration))Fail("Missing lexical source declaration.");
            }
        }
    }
    public static bool ValidMode(ModeDimension dimension,ModeChoice choice)=>dimension switch
    {
        ModeDimension.Configuration=>choice==ModeChoice.Fixed,
        ModeDimension.Logic=>choice is ModeChoice.And or ModeChoice.Or or ModeChoice.Xor or ModeChoice.Nor or ModeChoice.Nand,
        ModeDimension.OpticalChannel=>choice is ModeChoice.Broadband or ModeChoice.Red or ModeChoice.Green or ModeChoice.Blue or ModeChoice.Yellow or ModeChoice.Cyan or ModeChoice.Magenta or ModeChoice.White,
        ModeDimension.AcousticTone=>choice is ModeChoice.Low or ModeChoice.Mid or ModeChoice.High,
        ModeDimension.TubeAngle=>choice is ModeChoice.Degrees45 or ModeChoice.Degrees90,
        ModeDimension.Lifecycle=>choice is ModeChoice.Run or ModeChoice.Pause or ModeChoice.Step or ModeChoice.Reset or ModeChoice.Save or ModeChoice.Load,
        _=>false
    };
    private static void Text(string value){if(string.IsNullOrWhiteSpace(value))Fail("Missing inventory explanation.");}
    private static T[] Required<T>(T[]? values)=>values??throw new InvalidDataException("Null inventory array.");
    private static void NotNull(object? value){if(value is null)Fail("Null inventory record.");}
    private static void Unique<T>(IEnumerable<T> values)
    {
        var set=new HashSet<T>();
        foreach(var value in values)if(value is null||!set.Add(value))Fail("Null or duplicate inventory member.");
    }
    private static void Fail(string message)=>throw new InvalidDataException(message);
}
