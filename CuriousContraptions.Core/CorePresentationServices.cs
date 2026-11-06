using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CorePresentationServices : IPresentationServices
{
    public bool IsBlocked(ActuatorDriveRead drive) => drive.IsBlocked();
    public void ValidateBodies(PhysicsBodyReadSet bodies, IPhysicsSceneDeclaration scene, SimulationEpoch epoch, SimulationTick tick) => bodies.ValidateScene(scene, epoch, tick);
    public ulong TargetBase => WorkshopDeclaredPresentation.TargetBase;
    public IWorkshopDeclaredPresentation Create(IAnimationServices animation, ICompiledScene construction, WorkshopResponse endpoint) => new WorkshopDeclaredPresentation(animation, construction, endpoint);
    public IReadOnlyList<(AnimationTargetId Target, GpuBodyId Owner, int Binding)> Targets(ICompiledScene construction) => WorkshopDeclaredPresentation.Targets(construction);
    public PhysicalTime EventTime(SimulationEventRecord record) => WorkshopDeclaredPresentation.EventTime(record);
    public PresentationTimerPhase TimerPhase(ActivationTimerPhase phase) => WorkshopDeclaredPresentation.TimerPhase(phase);
    public AnimationProperty PropertyFor(PresentationProperty channel) => WorkshopDeclaredPresentation.Property(channel);
    public AnimationValue ValueFor(PresentationProperty channel, float value) => WorkshopDeclaredPresentation.Value(channel, value);
    public bool MatchesPlayback(WorkshopSimulationPhase phase, WorldPlayback playback) => WorkshopPoseHistory.MatchesPlayback(phase, playback);
}
