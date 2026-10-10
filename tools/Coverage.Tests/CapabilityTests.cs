using System.Text.Json;
using CuriousContraptions.Coverage;
using Xunit;

namespace CuriousContraptions.Coverage.Tests;

public sealed class CapabilityTests
{
    private const string Declaration="public class Sample { }";
    private static readonly WorkOrderId Owner=new("P0-002");
    private static readonly WorkOrderId OtherOwner=new("P0-034");
    private static readonly SourceKey Parent=new(RequirementOrigin.Task,new("sequence-task-001"));
    private static readonly SourceKey Child=new(RequirementOrigin.Element,new("element-001"));
    private static readonly ConsumerId ParentConsumer=new(new("task/sequence-task-001"));
    private static readonly ConsumerId ChildConsumer=new(new("element/element-001"));
    private static readonly EngineCapability[] RequiredCapabilities=[EngineCapability.ContactImpulse,EngineCapability.GeometryQuery];
    private static readonly SourceArtifact Artifact=new(new("parts/Sample.cs"),RequirementDiscovery.Hash(Declaration));
    private static readonly SourceRequirement[] Sources=
    [
        new(Parent,"docs/planning/requirements.md#sequence-task-001","Parent",RequirementDiscovery.Hash("parent")),
        new(Child,"docs/planning/requirements.md#element-001","Child",RequirementDiscovery.Hash("child"))
    ];
    private static CapabilityInventory Valid()
    {
        var capabilities=Enum.GetValues<EngineCapability>().Select(value=>new CapabilityContract(value,
            "Bounded independent test model.",[MeasurementUnit.Dimensionless],Owner,[Owner],Owner,
            CapabilityAvailability.Missing,[],"Independent test oracle.",
            value is EngineCapability.ContactImpulse or EngineCapability.GeometryQuery?[Parent,Child]:[],
            value==EngineCapability.ContactImpulse?[EngineCapability.GeometryQuery]:[])).ToArray();
        var consumers=new[]
        {
            new ConsumerContract(ParentConsumer,ConsumerKind.EngineService,Parent,[],RequiredCapabilities,Owner,[Artifact]),
            new ConsumerContract(ChildConsumer,ConsumerKind.FutureDeclaration,Child,[],RequiredCapabilities,Owner,[])
        };
        return new(capabilities,consumers,
        [
            new(Parent,Sources[0].Hash,ObligationKind.Engine,RequiredCapabilities,[ParentConsumer],[new(SourceRelationKind.CoverageScope,Child)],Owner,"Required engine scope."),
            new(Child,Sources[1].Hash,ObligationKind.FutureProduct,RequiredCapabilities,[ChildConsumer],[],Owner,"Named future declaration.")
        ],[new(Parent,ModeDimension.Configuration,ModeChoice.Fixed,Owner)]);
    }
    private static CapabilityExpectations Expected()=>new(new HashSet<WorkOrderId>{Owner,OtherOwner},
        Sources.ToDictionary(x=>x.Key,_=>Owner),
        Enum.GetValues<EngineCapability>().ToDictionary(x=>x,_=>new CapabilityOwners(Owner,[Owner],Owner)),
        [new(Parent,ModeDimension.Configuration,ModeChoice.Fixed,Owner)],
        new Dictionary<SourceKey,SourceRelation[]>{{Parent,[new(SourceRelationKind.CoverageScope,Child)]}},
        Enum.GetValues<EngineCapability>().ToDictionary(value=>value,value=>
            value==EngineCapability.ContactImpulse?new[]{EngineCapability.GeometryQuery}:Array.Empty<EngineCapability>()),
        new Dictionary<SourceKey,SourceArtifact[]>{{Parent,[Artifact]}},
        new Dictionary<SourceKey,EngineCapability[]>{{Parent,RequiredCapabilities},{Child,RequiredCapabilities}},
        new Dictionary<SourceKey,SourceSymbol[]>(),
        new Dictionary<SourceKey,SourceClassification>
        {
            [Parent]=new(ObligationKind.Engine,ConsumerKind.EngineService),
            [Child]=new(ObligationKind.FutureProduct,ConsumerKind.FutureDeclaration)
        },new Dictionary<EngineCapability,RequiredDeclaration[]>(),new HashSet<WorkOrderId>{Owner});
    private static CapabilitySummary Audit(CapabilityInventory value)=>CapabilityAudit.Analyze(Sources,value,Expected(),_=>Declaration);


    [Fact] public void ReviewedRoleInputPermitsStrictAuditButMissingOrConflictingRolesDoNot()
    {
        var expected = Expected();
        var roles = CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, expected.CapabilityOwners);
        Assert.Empty(roles.Issues);
        Assert.Contains(Owner, roles.Eligible);
        var result = CapabilityAudit.Analyze(Sources, Valid(), expected with { ImplementationEligibleOwners = roles.Eligible }, _ => Declaration);
        Assert.True(result.InventoryCurrent);
        Assert.False(result.RuntimeQualified);
        var empty = CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners,
            new Dictionary<EngineCapability, CapabilityOwners>());
        Assert.Equal(Enum.GetValues<EngineCapability>().Length, empty.Issues.Length);
        Assert.All(empty.Issues, issue => Assert.Equal(CurrentInputProblem.NoReviewedImplementationRole, issue.Problem));
        Assert.Empty(empty.Eligible);
        var wrong = expected.CapabilityOwners.ToDictionary(pair => pair.Key, pair => pair.Value);
        wrong[EngineCapability.ContactImpulse] = new(OtherOwner, [OtherOwner], OtherOwner);
        Assert.Contains(CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong).Issues,
            issue => issue.Problem == CurrentInputProblem.ConflictingImplementationRole);
        wrong[EngineCapability.ContactImpulse] = new(new("S999"), [new("S999")], new("S999"));
        Assert.Contains(CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong).Issues,
            issue => issue.Problem == CurrentInputProblem.UnknownOwner);
        wrong[EngineCapability.ContactImpulse] = new(Owner, [], Owner);
        Assert.Throws<InvalidDataException>(() => CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong));
        wrong[EngineCapability.ContactImpulse] = new(Owner, [Owner, Owner], Owner);
        Assert.Throws<InvalidDataException>(() => CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong));
        wrong[EngineCapability.ContactImpulse] = new(Owner, [default], Owner);
        Assert.Throws<ArgumentException>(() => CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong));
        wrong[EngineCapability.ContactImpulse] = expected.CapabilityOwners[EngineCapability.ContactImpulse];
        wrong[(EngineCapability)999] = new(Owner, [Owner], Owner);
        Assert.Throws<InvalidDataException>(() => CurrentImplementationRoles.Validate(expected.Owners, expected.CapabilityOwners, wrong));
        Assert.Throws<InvalidDataException>(() => CurrentImplementationRoles.Validate(expected.Owners,
            new Dictionary<EngineCapability, CapabilityOwners>(), expected.CapabilityOwners));
    }

    [Fact] public void CurrentInventoryCannotQualifyRuntime()
    {
        var result=Audit(Valid());
        Assert.True(result.InventoryCurrent);Assert.False(result.RuntimeQualified);
    }
    [Fact] public void RemovedNamedChildRejects()
    {
        var value=Valid();value.Bindings[0]=value.Bindings[0] with {Relations=[]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {BoundSources=[Parent]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid() with {Bindings=[Valid().Bindings[0]]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void RemovedModeAndWrongOwnerReject()
    {
        Assert.Throws<InvalidDataException>(()=>Audit(Valid() with {Modes=[]}));
        var value=Valid();value.Modes[0]=value.Modes[0] with {ProofOwner=OtherOwner};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void ExistingButUnrelatedOwnersRejectForEveryRecord()
    {
        var value=Valid();value.Bindings[0]=value.Bindings[0] with {ProofOwner=OtherOwner};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Consumers[0]=value.Consumers[0] with {ProofOwner=OtherOwner};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {ProofOwner=OtherOwner};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {DesignOwner=OtherOwner};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void StaleSourceIsIncompleteAndNotPass()
    {
        var value=Valid();value.Bindings[0]=value.Bindings[0] with {SourceHash=RequirementDiscovery.Hash("changed")};
        var result=Audit(value);Assert.False(result.InventoryCurrent);Assert.Equal(1,result.Changed);
    }
    [Fact] public void DuplicateSourceConsumerAndCapabilityReject()
    {
        var value=Valid();
        Assert.Throws<InvalidDataException>(()=>Audit(value with {Bindings=[..value.Bindings,value.Bindings[0]]}));
        Assert.Throws<InvalidDataException>(()=>Audit(value with {Consumers=[..value.Consumers,value.Consumers[0]]}));
        Assert.Throws<InvalidDataException>(()=>Audit(value with {Capabilities=[..value.Capabilities,value.Capabilities[0]]}));
    }
    [Fact] public void WrongConsumerAndCapabilityRelationReject()
    {
        var value=Valid();value.Bindings[0]=value.Bindings[0] with {Consumers=[ChildConsumer]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Consumers[0]=value.Consumers[0] with {Capabilities=[EngineCapability.ElectricalPower]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void MissingCapabilityDependencyAndCycleReject()
    {
        var value=Valid();value.Capabilities[0]=value.Capabilities[0] with {Dependencies=[EngineCapability.ElectricalPower]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value.Capabilities[(int)EngineCapability.ElectricalPower]=value.Capabilities[(int)EngineCapability.ElectricalPower]
            with {Dependencies=[EngineCapability.ContactImpulse]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        Assert.Throws<InvalidDataException>(()=>Audit(Valid() with {Capabilities=[]}));
    }
    [Fact] public void HashMatchDoesNotAdmitMissingDeclaration()
    {
        var value=Valid();var symbol=new SourceSymbol(new("engine/Sample.cs"),new("Sample"),RequirementDiscovery.Hash(Declaration));
        value.Consumers[0]=value.Consumers[0] with {Symbols=[symbol]};Assert.True(Audit(value).InventoryCurrent);
        value.Consumers[0]=value.Consumers[0] with {Symbols=[symbol with {Symbol=new("Absent")} ]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value.Consumers[0]=value.Consumers[0] with {Symbols=[symbol with {FileHash=RequirementDiscovery.Hash("old")} ]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void NullNestedValuesAndDefaultIdentitiesRejectCleanly()
    {
        var value=Valid();
        Assert.Throws<InvalidDataException>(()=>Audit(value with {Capabilities=null!}));
        Assert.Throws<InvalidDataException>(()=>Audit(value with {Consumers=[null!]}));
        value.Consumers[0]=value.Consumers[0] with {Artifacts=[null!]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {Dependencies=null!};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Consumers[0]=value.Consumers[0] with {Id=default};
        Assert.Throws<ArgumentException>(()=>Audit(value));
        value=Valid();value.Bindings[0]=value.Bindings[0] with {ProofOwner=default};
        Assert.Throws<ArgumentException>(()=>Audit(value));
    }
    [Fact] public void DimensionChoicePairsAreClosed()
    {
        Assert.True(CapabilityAudit.ValidMode(ModeDimension.Logic,ModeChoice.And));
        Assert.False(CapabilityAudit.ValidMode(ModeDimension.Logic,ModeChoice.Red));
        Assert.False(CapabilityAudit.ValidMode((ModeDimension)999,ModeChoice.Fixed));
        var value=Valid();value.Modes[0]=value.Modes[0] with {Dimension=ModeDimension.Logic,Choice=ModeChoice.Red};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void EveryCanonicalEnumRoundTripsAndUnsupportedBoundaryRejects()
    {
        RoundTrip<SourceRelationKind>();RoundTrip<EngineCapability>();RoundTrip<ObligationKind>();RoundTrip<ConsumerKind>();
        RoundTrip<CapabilityAvailability>();RoundTrip<MeasurementUnit>();RoundTrip<ModeDimension>();RoundTrip<ModeChoice>();
        var encoded=JsonSerializer.Serialize(Valid(),CoverageJson.Options);
        var decoded=JsonSerializer.Deserialize<CapabilityInventory>(encoded,CoverageJson.Options)!;
        Assert.True(Audit(decoded).InventoryCurrent);
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CapabilityInventory>("{\"unexpected\":true}",CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<EngineCapability>("\"contactimpulse\"",CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<EngineCapability>("999",CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((EngineCapability)999,CoverageJson.Options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SourceRelationKind>("\"Unknown\"",CoverageJson.Options));
        static void RoundTrip<T>() where T:struct,Enum
        {
            foreach(var value in Enum.GetValues<T>())
                Assert.Equal(value,JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,CoverageJson.Options),CoverageJson.Options));
        }
    }
    [Fact] public void ExtensibleWorkOrdersAndPathsRemainValidated()
    {
        Assert.Equal("FIX-001-001",new WorkOrderId("FIX-001-001").Value);
        Assert.Equal("ENGINE-DEVICE-01",new WorkOrderId("ENGINE-DEVICE-01").Value);
        Assert.Equal("CLEAN-CONTEXT",new WorkOrderId("CLEAN-CONTEXT").Value);
        Assert.Throws<ArgumentException>(()=>new WorkOrderId("p0-001"));
        Assert.Throws<ArgumentException>(()=>new SourcePath("../outside.json"));
        Assert.Throws<ArgumentException>(()=>new SourcePath("/outside.json"));
        Assert.Throws<ArgumentException>(()=>new SymbolId("invalid symbol"));
    }

    [Fact] public void SourceOwnersFollowAnchorsRatherThanNumberOffsets()
    {
        const string document="<a id=\"element-168\"></a>\n<a id=\"sequence-task-017\"></a>\nFirst\n"+
            "<a id=\"gap-01\"></a>\nScope index prose\n<a id=\"todo-999\"></a>\n"+
            "<a id=\"sequence-task-721\"></a>\nSecond";
        var result=CapabilityInputs.SourceTaskAnchors(document);
        Assert.Equal(new SourceKey(RequirementOrigin.Task,new("sequence-task-017")),
            result[new(RequirementOrigin.Element,new("element-168"))]);
        Assert.Equal(new SourceKey(RequirementOrigin.Task,new("sequence-task-721")),
            result[new(RequirementOrigin.Gap,new("gap-01"))]);
    }
    [Fact] public void MissingDuplicateAndInterruptedSourceAnchorsReject()
    {
        const string missing="<a id=\"element-001\"></a>\nNo task";
        const string interrupted="<a id=\"element-001\"></a>\n<a id=\"unrelated-section\"></a>\n"+
            "<a id=\"sequence-task-554\"></a>";
        const string duplicate="<a id=\"element-001\"></a>\n<a id=\"sequence-task-554\"></a>\n"+
            "<a id=\"element-002\"></a>\n<a id=\"sequence-task-554\"></a>";
        Assert.Throws<InvalidDataException>(()=>CapabilityInputs.SourceTaskAnchors(missing));
        Assert.Throws<InvalidDataException>(()=>CapabilityInputs.SourceTaskAnchors(interrupted));
        Assert.Throws<InvalidDataException>(()=>CapabilityInputs.SourceTaskAnchors(duplicate));
    }

    [Fact] public void RemovedRequiredDependencyAndArtifactReject()
    {
        Assert.True(Audit(Valid()).InventoryCurrent);
        var value=Valid();
        value.Capabilities[0]=value.Capabilities[0] with {Dependencies=[]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Consumers[0]=value.Consumers[0] with {Artifacts=[]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Bindings[0]=value.Bindings[0] with {Capabilities=[EngineCapability.GeometryQuery]};
        value.Consumers[0]=value.Consumers[0] with {Capabilities=[EngineCapability.GeometryQuery]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void RequiredResearchChildrenAndLawContractsAreNotInventoryDerived()
    {
        var research=CapabilityRequirements.NamedChildren.Where(pair=>pair.Key.Origin==RequirementOrigin.Research).ToArray();
        Assert.Equal(5,research.Length);
        Assert.All(research,pair=>Assert.NotEmpty(pair.Value));
        Assert.Equal(Enum.GetValues<EngineCapability>().Length,CapabilityRequirements.Dependencies.Count);
        Assert.Contains(EngineCapability.ReliablePublication,
            CapabilityRequirements.Dependencies[EngineCapability.PresentationHistory]);
        Assert.Contains(EngineCapability.SlidingFriction,CapabilityRequirements.ContactConsumerRequirements);
    }

    [Fact] public void WrongValidKindsAndPartialWithoutSymbolsReject()
    {
        Assert.True(Audit(Valid()).InventoryCurrent);
        var value=Valid();
        value.Consumers[0]=value.Consumers[0] with {Kind=ConsumerKind.FutureDeclaration};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Bindings[0]=value.Bindings[0] with {Kind=ObligationKind.ProductWorkflow};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Capabilities[0]=value.Capabilities[0] with {Availability=CapabilityAvailability.Partial};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }
    [Fact] public void FrozenTaskRequirementsRetainDistinctSampledSemantics()
    {
        Assert.Equal(799,CapabilityTaskRequirements.Values.Count);
        var motor=CapabilityTaskRequirements.Values[new(RequirementOrigin.Task,new("sequence-task-194"))];
        Assert.Contains(EngineCapability.ShaftTorque,motor.Capabilities);
        Assert.Contains(EngineCapability.ElectricalPower,motor.Capabilities);
        var basket=CapabilityTaskRequirements.Values[new(RequirementOrigin.Task,new("sequence-task-080"))];
        Assert.Contains(EngineCapability.ObjectiveEvaluation,basket.Capabilities);
        Assert.Contains(EngineCapability.GeometryQuery,basket.Capabilities);
        var lesson=CapabilityTaskRequirements.Values[new(RequirementOrigin.Task,new("sequence-task-334"))];
        Assert.Equal(ObligationKind.ProductWorkflow,lesson.Kind);
        Assert.Contains(new SourceKey(RequirementOrigin.Catalogue,new("beam_splitter")),lesson.Children);
        Assert.DoesNotContain(EngineCapability.OpticalTransport,lesson.Capabilities);
    }

    [Fact] public void DuplicateLogicalConsumerAndFabricatedSourceRelationReject()
    {
        var value=Valid();
        var clone=value.Consumers[0] with {Id=new(new("task/duplicate"))};
        value=value with {Consumers=[..value.Consumers,clone]};
        value.Bindings[0]=value.Bindings[0] with {Consumers=[ParentConsumer,clone.Id]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Bindings[1]=value.Bindings[1] with {Relations=[new(SourceRelationKind.SourceReference,Parent)]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Bindings[0]=value.Bindings[0] with {Relations=[new(SourceRelationKind.SourceReference,Child)]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();
        value.Bindings[0]=value.Bindings[0] with {Relations=[new((SourceRelationKind)999,Child)]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
    }

    [Fact] public void CanonicalEnumBoundaryRejectsReorderedRenamedAndChangedValues()
    {
        var expected=new Dictionary<SymbolId,int>{[new("And")]=0,[new("Or")]=1};
        CapabilityInputs.ValidateEnumDeclaration("public enum LogicGateKind { And, Or }",new("LogicGateKind"),expected);
        CapabilityInputs.ValidateEnumDeclaration("public enum LogicGateKind { And = 0, Or = 1 }",new("LogicGateKind"),expected);
        foreach(var declaration in new[]
        {
            "public enum LogicGateKind { Or, And }",
            "public enum LogicGateKind { And = 1, Or = 2 }",
            "public enum LogicGateKind { And, Xor }",
            "public enum LogicGateKind { And, Or, Xor }",
            "public enum LogicGateKind { And = Other, Or }",
            "public enum LogicGateKind { And = 2147483647, Or }",
            "public enum LogicGateKind { And = 9999999999999999999999999999999999999, Or }"
        })
            Assert.Throws<InvalidDataException>(()=>CapabilityInputs.ValidateEnumDeclaration(declaration,new("LogicGateKind"),expected));
    }

    [Fact] public void ImplementationOwnerSetsRejectMissingDuplicateAuditAndOldSingularSchema()
    {
        var value=Valid();value.Capabilities[0]=value.Capabilities[0] with {ImplementationOwners=[]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {ImplementationOwners=[Owner,Owner]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {ImplementationOwners=[OtherOwner]};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        value=Valid();value.Capabilities[0]=value.Capabilities[0] with {ImplementationOwners=null!};
        Assert.Throws<InvalidDataException>(()=>Audit(value));
        Assert.Throws<InvalidDataException>(()=>CapabilityAudit.Analyze(Sources,Valid(),
            Expected() with {ImplementationEligibleOwners=new HashSet<WorkOrderId>()},_=>Declaration));
        var encoded=JsonSerializer.Serialize(Valid(),CoverageJson.Options)
            .Replace("\"ImplementationOwners\"","\"ImplementationOwner\"");
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CapabilityInventory>(encoded,CoverageJson.Options));
    }
}
