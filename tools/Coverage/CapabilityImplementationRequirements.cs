namespace CuriousContraptions.Coverage;

public sealed record RequiredDeclaration(SourcePath Path,SymbolId Symbol);
public static class CapabilityImplementationRequirements
{
    public static readonly IReadOnlyDictionary<EngineCapability,RequiredDeclaration[]> CurrentDeclarations=
        new Dictionary<EngineCapability,RequiredDeclaration[]>
        {
            [EngineCapability.TopologyTransaction]=[new(new("engine/physics/PhysicsWorld.cs"),new("PhysicsWorld"))],
            [EngineCapability.PresentationHistory]=[new(new("engine/bridge/CommittedPoseBuffer.cs"),new("CommittedPoseBuffer"))],
            [EngineCapability.RenderApplication]=[new(new("engine/presentation/SceneAnimationAdapter.cs"),new("SceneAnimationAdapter"))],
            [EngineCapability.PerformanceMeasurement]=[new(new("engine/PerformanceRecorder.cs"),new("PerformanceRecorder"))],
            [EngineCapability.ConstructionPersistence]=[new(new("engine/MachineData.cs"),new("MachineData"))],
            [EngineCapability.DifficultyAssistance]=[new(new("engine/PartAssistance.cs"),new("PartAssistance"))],
            [EngineCapability.TypedContracts]=[new(new("engine/ConnectionPort.cs"),new("ConnectionPort"))],
            [EngineCapability.RigidBodyDynamics]=[new(new("engine/physics/PhysicsWorld.cs"),new("PhysicsWorld"))],
            [EngineCapability.EnvironmentState]=[new(new("engine/physics/PhysicsWorld.cs"),new("PhysicsWorld"))],
            [EngineCapability.ConstructionCompiler]=[new(new("engine/ScenePhysicsAssembly.cs"),new("ScenePhysicsAssembly"))],
            [EngineCapability.StateTransaction]=[new(new("engine/SimulationTransaction.cs"),new("SimulationTransaction"))],
            [EngineCapability.FiniteLedger]=[new(new("engine/physics/PhysicsEnergyStore.cs"),new("PhysicsEnergyStoreDeclaration"))],
            [EngineCapability.TimedCommand]=[new(new("engine/bridge/SimulationCommandInbox.cs"),new("SimulationCommandInbox")),new(new("engine/SimulationTimers.cs"),new("SimulationTimers"))],
            [EngineCapability.ReliablePublication]=[new(new("engine/bridge/CommittedPoseBuffer.cs"),new("CommittedPoseBuffer"))],
            [EngineCapability.SimulationClock]=[new(new("engine/MachineWorld.cs"),new("MachineWorld"))],
            [EngineCapability.AnimationEvaluation]=[new(new("engine/presentation/AnimationBatch.cs"),new("AnimationBatch"))],
            [EngineCapability.AnimationLifecycle]=[new(new("engine/presentation/SceneAnimationAdapter.cs"),new("SceneAnimationAdapter"))],
            [EngineCapability.ElasticStorage]=[new(new("engine/physics/AxialElasticLoad.cs"),new("AxialElasticLoad"))],
            [EngineCapability.FiniteWorkActuation]=[new(new("engine/physics/PhysicsMotorCommand.cs"),new("PhysicsMotorCommand"))],
            [EngineCapability.GeometryQuery]=[new(new("engine/WorldGeometry.cs"),new("WorldGeometry"))],
            [EngineCapability.GasState]=[new(new("engine/physics/AxialGasLoad.cs"),new("AxialGasLoad"))],
            [EngineCapability.AerodynamicDrag]=[new(new("engine/physics/BodyDragLoad.cs"),new("BodyDragLoad"))],
            [EngineCapability.AcousticPropagation]=[new(new("engine/Acoustics.cs"),new("AcousticNetwork"))],
            [EngineCapability.OpticalTransport]=[new(new("engine/OpticalNetwork.cs"),new("OpticalNetwork"))],
            [EngineCapability.ContactImpulse]=[new(new("engine/physics/ContactConstraint.cs"),new("ContactConstraint"))],
            [EngineCapability.SlidingFriction]=[new(new("engine/physics/ContactConstraint.cs"),new("ContactConstraint"))],
            [EngineCapability.JointConstraint]=[new(new("engine/physics/PhysicsJoint.cs"),new("PhysicsJoint"))],
            [EngineCapability.TensionTransmission]=[new(new("engine/physics/PhysicsRopeJoint.cs"),new("PhysicsRopeJoint"))],
            [EngineCapability.ShaftTorque]=[new(new("engine/physics/PhysicsTransmissionJoint.cs"),new("PhysicsTransmissionJoint"))],
            [EngineCapability.ElectricalPower]=[new(new("engine/ElectricalNetwork.cs"),new("ElectricalNetwork"))],
            [EngineCapability.SignalPropagation]=[new(new("engine/BinaryCircuit.cs"),new("BinaryCircuit"))],
            [EngineCapability.Buoyancy]=[new(new("parts/BallPart.cs"),new("BallPart"))],
        };
}

