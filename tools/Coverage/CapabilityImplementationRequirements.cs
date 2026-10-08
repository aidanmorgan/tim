namespace CuriousContraptions.Coverage;

public sealed record RequiredDeclaration(SourcePath Path,SymbolId Symbol);
public static class CapabilityImplementationRequirements
{
    public static readonly IReadOnlyDictionary<EngineCapability,RequiredDeclaration[]> CurrentDeclarations=
        new Dictionary<EngineCapability,RequiredDeclaration[]>
        {
            [EngineCapability.TopologyTransaction]=[new(new("engine/gpu/WorkshopSimulation.cs"),new("WorkshopSimulation")),new(new("engine/gpu/PhysicsGpuAbi.cs"),new("PhysicsGpuAbi"))],
            [EngineCapability.PresentationHistory]=[new(new("engine/gpu/WorkshopPoseHistory.cs"),new("WorkshopPoseHistory")),new(new("engine/gpu/PhysicsMotionRead.cs"),new("PhysicsMotionRead"))],
            [EngineCapability.RenderApplication]=[new(new("engine/MachineWorld.Gpu.cs"),new("MachineWorld"))],
            [EngineCapability.PerformanceMeasurement]=[new(new("engine/PerformanceRecorder.cs"),new("PerformanceRecorder"))],
            [EngineCapability.ConstructionPersistence]=[new(new("engine/MachineData.cs"),new("MachineData"))],
            [EngineCapability.DifficultyAssistance]=[new(new("engine/PartAssistance.cs"),new("PartAssistance"))],
            [EngineCapability.TypedContracts]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("PhysicsSceneDeclaration")),new(new("engine/gpu/WorkshopWire.cs"),new("WorkshopWire"))],
            [EngineCapability.RigidBodyDynamics]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("RigidBodyDeclaration"))],
            [EngineCapability.EnvironmentState]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("PhysicsSceneDeclaration"))],
            [EngineCapability.ConstructionCompiler]=[new(new("engine/gpu/WorkshopPhysicsCompiler.cs"),new("WorkshopPhysicsCompiler"))],
            [EngineCapability.StateTransaction]=[new(new("CuriousContraptions.Simulation/WorkshopGpuDevice.cs"),new("WorkshopGpuDevice")),new(new("engine/gpu/WorkshopSimulation.cs"),new("WorkshopSimulation"))],
            [EngineCapability.FiniteLedger]=[new(new("engine/physics/PhysicsEnergyStore.cs"),new("PhysicsEnergyStoreDeclaration"))],
            [EngineCapability.TimedCommand]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program"))],
            [EngineCapability.ReliablePublication]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program")),new(new("engine/gpu/BrowserWorkshopClient.cs"),new("BrowserWorkshopClient"))],
            [EngineCapability.SimulationClock]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program")),new(new("engine/gpu/WorkshopCadence.cs"),new("WorkshopCadenceSettings")),new(new("engine/gpu/WorkshopClockMapping.cs"),new("WorkshopClockMapping"))],
            [EngineCapability.AnimationEvaluation]=[new(new("engine/presentation/AnimationBatch.cs"),new("AnimationBatch"))],
            [EngineCapability.AnimationLifecycle]=[new(new("CuriousContraptions.Animation.Worker/Program.cs"),new("Program")),new(new("engine/gpu/BrowserWorkshopClient.cs"),new("BrowserWorkshopClient"))],
            [EngineCapability.ElasticStorage]=[new(new("engine/physics/AxialElasticLoad.cs"),new("AxialElasticLoad"))],
            [EngineCapability.FiniteWorkActuation]=[new(new("engine/physics/PhysicsMotorCommand.cs"),new("PhysicsMotorCommand"))],
            [EngineCapability.GeometryQuery]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ColliderDeclaration"))],
            [EngineCapability.GasState]=[new(new("engine/physics/AxialGasLoad.cs"),new("AxialGasLoad"))],
            [EngineCapability.AerodynamicDrag]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("RigidBodyDeclaration"))],
            [EngineCapability.AcousticPropagation]=[new(new("engine/Acoustics.cs"),new("AcousticNetwork"))],
            [EngineCapability.OpticalTransport]=[new(new("engine/OpticalNetwork.cs"),new("OpticalNetwork"))],
            [EngineCapability.ContactImpulse]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ContactMaterialDeclaration"))],
            [EngineCapability.SlidingFriction]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ContactMaterialDeclaration"))],
            [EngineCapability.JointConstraint]=[new(new("engine/physics/PhysicsJoint.cs"),new("PhysicsJoint"))],
            [EngineCapability.TensionTransmission]=[new(new("engine/physics/PhysicsRopeJoint.cs"),new("PhysicsRopeJoint"))],
            [EngineCapability.ShaftTorque]=[new(new("engine/physics/PhysicsTransmissionJoint.cs"),new("PhysicsTransmissionJoint"))],
            [EngineCapability.ElectricalPower]=[new(new("engine/ElectricalNetwork.cs"),new("ElectricalNetwork"))],
            [EngineCapability.SignalPropagation]=[new(new("engine/BinaryCircuit.cs"),new("BinaryCircuit"))],
            [EngineCapability.Buoyancy]=[new(new("parts/BallPart.cs"),new("BallPart"))],
        };
}

