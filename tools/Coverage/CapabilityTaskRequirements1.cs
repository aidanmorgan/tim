namespace CuriousContraptions.Coverage;

// Required source-clause obligations, independent of candidate inventory data.
public static partial class CapabilityTaskRequirements
{
    private static readonly RequiredTaskContract[] Group1=
    [
        new(new("sequence-task-001"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.TypedContracts],RequiredTaskScope.DeclarationsAndFixtures,
            [

            ]),
        new(new("sequence-task-002"),ObligationKind.Engine,
            [EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-003"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-004"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-005"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.PerformanceMeasurement,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.SimulationClock,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-006"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-007"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-008"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.GeometryQuery,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-009"),ObligationKind.Engine,
            [EngineCapability.GeometryQuery],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-010"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.GeometryQuery,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-011"),ObligationKind.Engine,
            [EngineCapability.GeometryQuery,EngineCapability.PerformanceMeasurement],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-012"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-013"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.DependencyScheduling,EngineCapability.GeometryQuery,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-014"),ObligationKind.Engine,
            [EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-015"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.DependencyScheduling,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-016"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-017"),ObligationKind.Engine,
            [EngineCapability.AerodynamicDrag,EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.ElasticStorage,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ObjectiveEvaluation,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.StateTransaction,EngineCapability.TensionTransmission,EngineCapability.TimedCommand,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("rope_anchor")),
                new(RequirementOrigin.Catalogue,new("pulley")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("spring")),
                new(RequirementOrigin.Catalogue,new("fan")),
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("basket"))
            ]),
        new(new("sequence-task-018"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-019"),ObligationKind.Engine,
            [EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.JointConstraint,EngineCapability.ResourceLifetime,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.SimulationClock,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-020"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-021"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-022"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-023"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-024"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-025"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.TypedContracts],RequiredTaskScope.DeclarationsAndFixtures,
            [

            ]),
        new(new("sequence-task-026"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.GeometryQuery,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-027"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.DependencyScheduling,EngineCapability.FiniteLedger,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-028"),ObligationKind.Engine,
            [EngineCapability.ChemicalReaction,EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.DependencyScheduling,EngineCapability.ElectricalDissipation,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.FluidAdvection,EngineCapability.GasState,EngineCapability.GeometryQuery,EngineCapability.IonizingTransport,EngineCapability.JointConstraint,EngineCapability.OpticalAbsorption,EngineCapability.OpticalTransport,EngineCapability.PhaseTopology,EngineCapability.PressureWork,EngineCapability.RigidBodyDynamics,EngineCapability.SensibleHeat,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.SolidMelting,EngineCapability.StateTransaction,EngineCapability.ThermalConduction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Task,new("sequence-task-476")),
                new(RequirementOrigin.Task,new("sequence-task-477")),
                new(RequirementOrigin.Task,new("sequence-task-478")),
                new(RequirementOrigin.Task,new("sequence-task-479")),
                new(RequirementOrigin.Task,new("sequence-task-480")),
                new(RequirementOrigin.Task,new("sequence-task-484")),
                new(RequirementOrigin.Task,new("sequence-task-485")),
                new(RequirementOrigin.Task,new("sequence-task-486")),
                new(RequirementOrigin.Task,new("sequence-task-489")),
                new(RequirementOrigin.Task,new("sequence-task-492")),
                new(RequirementOrigin.Task,new("sequence-task-499")),
                new(RequirementOrigin.Task,new("sequence-task-510")),
                new(RequirementOrigin.Task,new("sequence-task-512")),
            ]),
        new(new("sequence-task-029"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.PerformanceMeasurement,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.SimulationClock,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-030"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.DependencyScheduling,EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-031"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.TypedContracts],RequiredTaskScope.DeclarationsAndFixtures,
            [

            ]),
        new(new("sequence-task-032"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-033"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.DependencyScheduling,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-034"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-035"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-036"),ObligationKind.Engine,
            [EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-037"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-038"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-039"),ObligationKind.Engine,
            [EngineCapability.InputAdmission,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-040"),ObligationKind.Engine,
            [EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-041"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-042"),ObligationKind.Engine,
            [EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-043"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-044"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-045"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-046"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-047"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.TypedContracts],RequiredTaskScope.DeclarationsAndFixtures,
            [

            ]),
        new(new("sequence-task-048"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-049"),ObligationKind.Engine,
            [EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-050"),ObligationKind.Engine,
            [EngineCapability.ReliablePublication,EngineCapability.ResourceLifetime,EngineCapability.SimulationClock,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-051"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-052"),ObligationKind.Engine,
            [EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-053"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.ResourceLifetime,EngineCapability.StateTransaction,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-054"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-055"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.AnimationLifecycle,EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.SimulationClock,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-056"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ResourceLifetime,EngineCapability.TypedContracts,EngineCapability.WorkerTransport],RequiredTaskScope.Declarations,
            [

            ]),
        new(new("sequence-task-057"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SlidingFriction,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("impact_lever")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-058"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SlidingFriction,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("impact_lever")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-059"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SlidingFriction,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("impact_lever")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-060"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PerformanceMeasurement,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-061"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PerformanceMeasurement,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-062"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ContactImpulse,EngineCapability.DependencyScheduling,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PerformanceMeasurement,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-063"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("clutch")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("conveyor")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-064"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("clutch")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("conveyor")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-065"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("clutch")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("conveyor")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-066"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.InputAdmission,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-067"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-068"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-069"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Catalogue,
            [

            ]),
        new(new("sequence-task-070"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-071"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Catalogue,
            [

            ]),
        new(new("sequence-task-072"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-073"),ObligationKind.Engine,
            [EngineCapability.PerformanceMeasurement,EngineCapability.ReliablePublication,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.Catalogue,
            [

            ]),
        new(new("sequence-task-074"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("clutch")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("conveyor")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-075"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("motor")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("clutch")),
                new(RequirementOrigin.Catalogue,new("reverse_transmission")),
                new(RequirementOrigin.Catalogue,new("conveyor")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-076"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pressure_plate")),
            ]),
        new(new("sequence-task-077"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("domino")),
            ]),
        new(new("sequence-task-078"),ObligationKind.Engine,
            [EngineCapability.AnimationEvaluation,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("wound_spring")),
            ]),
        new(new("sequence-task-079"),ObligationKind.Engine,
            [EngineCapability.DifficultyAssistance,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ObjectiveEvaluation,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("basket")),
                new(RequirementOrigin.Catalogue,new("ball_detector")),
            ]),
        new(new("sequence-task-080"),ObligationKind.Engine,
            [EngineCapability.DifficultyAssistance,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ObjectiveEvaluation,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("basket")),
                new(RequirementOrigin.Catalogue,new("ball_detector")),
            ]),
        new(new("sequence-task-081"),ObligationKind.Engine,
            [EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.ObjectiveEvaluation,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("ball_detector")),
                new(RequirementOrigin.Catalogue,new("counter")),
                new(RequirementOrigin.Catalogue,new("basket")),
            ]),
        new(new("sequence-task-082"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("cannon")),
            ]),
        new(new("sequence-task-083"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("cannon")),
            ]),
        new(new("sequence-task-084"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-085"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElasticStorage,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.ResourceLifetime,EngineCapability.RigidBodyDynamics,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("trampoline")),
            ]),
        new(new("sequence-task-086"),ObligationKind.Engine,
            [EngineCapability.DifficultyAssistance,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ObjectiveEvaluation,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("basket")),
                new(RequirementOrigin.Catalogue,new("ball_detector")),
            ]),
        new(new("sequence-task-087"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-088"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-089"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-090"),ObligationKind.Engine,
            [EngineCapability.ElasticStorage,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.OpticalTransport,EngineCapability.PresentationHistory,EngineCapability.ReliablePublication,EngineCapability.RenderApplication,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("powered_gate")),
                new(RequirementOrigin.Catalogue,new("beam_shutter")),
            ]),
        new(new("sequence-task-091"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("pipe")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_45")),
                new(RequirementOrigin.Catalogue,new("pipe_bend_90")),
                new(RequirementOrigin.Catalogue,new("funnel")),
                new(RequirementOrigin.Catalogue,new("linear_pusher")),
            ]),
        new(new("sequence-task-092"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-093"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-094"),ObligationKind.Engine,
            [EngineCapability.ConstructionCompiler,EngineCapability.ConstructionPersistence,EngineCapability.EnvironmentState,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.ReliablePublication,EngineCapability.RigidBodyDynamics,EngineCapability.StateTransaction,EngineCapability.TopologyTransaction,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-095"),ObligationKind.Engine,
            [EngineCapability.AcousticPropagation,EngineCapability.AerodynamicDrag,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PressureWork,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("fan")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-096"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SimulationClock,EngineCapability.SlidingFriction,EngineCapability.StateTransaction,EngineCapability.TimedCommand,EngineCapability.TypedContracts],RequiredTaskScope.NamedSources,
            [

            ]),
        new(new("sequence-task-097"),ObligationKind.Engine,
            [EngineCapability.AcousticPropagation,EngineCapability.AerodynamicDrag,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PressureWork,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("fan")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-098"),ObligationKind.Engine,
            [EngineCapability.AcousticPropagation,EngineCapability.AerodynamicDrag,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PressureWork,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("fan")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-099"),ObligationKind.Engine,
            [EngineCapability.AcousticPropagation,EngineCapability.AerodynamicDrag,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.PressureWork,EngineCapability.RigidBodyDynamics,EngineCapability.ShaftTorque,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("fan")),
                new(RequirementOrigin.Catalogue,new("windmill")),
                new(RequirementOrigin.Catalogue,new("bellows")),
                new(RequirementOrigin.Catalogue,new("wind_chimes")),
            ]),
        new(new("sequence-task-100"),ObligationKind.Engine,
            [EngineCapability.ContactImpulse,EngineCapability.ElectricalPower,EngineCapability.EnvironmentState,EngineCapability.FiniteLedger,EngineCapability.FiniteWorkActuation,EngineCapability.GeometryQuery,EngineCapability.JointConstraint,EngineCapability.RigidBodyDynamics,EngineCapability.SignalPropagation,EngineCapability.StateTransaction],RequiredTaskScope.NamedSources,
            [
                new(RequirementOrigin.Catalogue,new("cannon")),
            ]),
    ];
}
