namespace CuriousContraptions.Coverage;

public sealed record RequiredDeclaration(SourcePath Path,SymbolId Symbol);
// No declaration entry means no currently claimed implementation symbol; capability obligations remain required.
public static class CapabilityImplementationRequirements
{
    public static readonly IReadOnlyDictionary<EngineCapability,RequiredDeclaration[]> CurrentDeclarations=
        new Dictionary<EngineCapability,RequiredDeclaration[]>
        {
            [EngineCapability.TopologyTransaction]=[new(new("engine/gpu/WorkshopSimulation.cs"),new("WorkshopSimulation")),new(new("engine/gpu/PhysicsGpuAbi.cs"),new("PhysicsGpuAbi"))],
            [EngineCapability.PresentationHistory]=[new(new("engine/gpu/WorkshopPoseHistory.cs"),new("WorkshopPoseHistory")),new(new("engine/gpu/PhysicsMotionRead.cs"),new("PhysicsMotionRead"))],
            [EngineCapability.RenderApplication]=[new(new("engine/MachineWorld.Gpu.cs"),new("MachineWorld"))],
            [EngineCapability.TypedContracts]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("PhysicsSceneDeclaration")),new(new("engine/gpu/WorkshopWire.cs"),new("WorkshopWire"))],
            [EngineCapability.RigidBodyDynamics]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("RigidBodyDeclaration"))],
            [EngineCapability.EnvironmentState]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("PhysicsSceneDeclaration"))],
            [EngineCapability.ConstructionCompiler]=[new(new("engine/gpu/WorkshopPhysicsCompiler.cs"),new("WorkshopPhysicsCompiler"))],
            [EngineCapability.StateTransaction]=[new(new("CuriousContraptions.Simulation/WorkshopGpuDevice.cs"),new("WorkshopGpuDevice")),new(new("engine/gpu/WorkshopSimulation.cs"),new("WorkshopSimulation"))],
            [EngineCapability.TimedCommand]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program"))],
            [EngineCapability.ReliablePublication]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program")),new(new("engine/gpu/BrowserWorkshopClient.cs"),new("BrowserWorkshopClient"))],
            [EngineCapability.SimulationClock]=[new(new("CuriousContraptions.Simulation/Program.cs"),new("Program")),new(new("engine/gpu/WorkshopCadence.cs"),new("WorkshopCadenceSettings")),new(new("engine/gpu/WorkshopClockMapping.cs"),new("WorkshopClockMapping"))],
            [EngineCapability.AnimationEvaluation]=[new(new("engine/presentation/AnimationBatch.cs"),new("AnimationBatch"))],
            [EngineCapability.AnimationLifecycle]=[new(new("CuriousContraptions.Animation.Worker/Program.cs"),new("Program")),new(new("engine/gpu/BrowserWorkshopClient.cs"),new("BrowserWorkshopClient"))],
            [EngineCapability.GeometryQuery]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ColliderDeclaration"))],
            [EngineCapability.AerodynamicDrag]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("RigidBodyDeclaration"))],
            [EngineCapability.ContactImpulse]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ContactMaterialDeclaration"))],
            [EngineCapability.SlidingFriction]=[new(new("engine/gpu/PhysicsDeclarations.cs"),new("ContactMaterialDeclaration"))],
            [EngineCapability.Buoyancy]=[new(new("parts/BallPart.cs"),new("BallPart"))],
        };
}

