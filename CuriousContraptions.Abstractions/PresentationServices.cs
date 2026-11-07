using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public enum AnimationServiceTurn { Wait, ApplyDeferred, Evaluate }
public readonly record struct AnimationControlApplication(float Value, bool Started);
public readonly record struct DeclaredPresentationSample(
    AnimationTargetId Target, GpuBodyId Owner, int Binding, AnimationValue Value);

public interface IAnimationControlDeferral
{
    IWorkshopDiagnostics Diagnostics { get; }
    bool IsHolding { get; }
    ulong Deferrals { get; }
    void Hold(byte[] control, AnimationDeferralCause cause);
    AnimationServiceTurn Turn(bool installed, bool preparing, bool masterTimeAvailable);
    byte[]? Release();
}

public interface IWorkshopAnimationTracks
{
    int Capacity { get; }
    void Validate(WorkshopAnimationControl control);
    AnimationControlApplication Apply(WorkshopAnimationControl control, PhysicalTime masterTime);
    void Advance(ulong sequence, PhysicalTime presentationTime);
    bool TryRead(int slot, out WorkshopAnimationControl control, out float value);
    void RetireWorld(SimulationEpoch current);
}

public interface IWorkshopDeclaredPresentation
{
    ulong Lapped { get; }
    void AcceptEvents(EventRingDelivery delivery, WorkshopWorldProjection world);
    void Accept(WorkshopResponse response);
    bool CanRetain(ICompiledScene construction, WorkshopResponse endpoint);
    void ValidateRebind(ICompiledScene construction, WorkshopResponse endpoint);
    void Rebind(ICompiledScene construction, WorkshopResponse endpoint);
    IReadOnlyList<DeclaredPresentationSample> Advance(WorkshopSchedule schedule, PhysicalTime master);
    IReadOnlyList<DeclaredPresentationSample> Advance(PhysicalTime worldTime);
    void SetVisible(AnimationTargetId target, AnimationProperty channel, bool visible);
}

public interface IPresentationServices
{
    bool IsBlocked(ActuatorDriveRead drive);
    void ValidateBodies(PhysicsBodyReadSet bodies, IPhysicsSceneDeclaration scene, SimulationEpoch epoch, SimulationTick tick);
    ulong TargetBase { get; }
    IWorkshopDeclaredPresentation Create(IAnimationServices animation, ICompiledScene construction, WorkshopResponse endpoint);
    IReadOnlyList<(AnimationTargetId Target, GpuBodyId Owner, int Binding)> Targets(ICompiledScene construction);
    PhysicalTime EventTime(SimulationEventRecord record);
    AnimationProperty PropertyFor(PresentationProperty channel);
    AnimationValue ValueFor(PresentationProperty channel, float value);
    bool MatchesPlayback(WorkshopSimulationPhase phase, WorldPlayback playback);
}
