using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

public interface IAnimationServices
{
    IAnimationBatch CreateBatch();
}

public interface IAnimationBatch
{
    AnimationGeneration Generation { get; }
    int RegisteredCount { get; }
    int ScheduledCount { get; }
    ReadOnlySpan<AnimationSample> Samples { get; }
    int Capacity { get; }
    void EnsureCapacity(int capacity);
    IAnimationHandle Register(AnimationBinding binding,AnimationDefinition definition);
    IAnimationHandle RegisterFollow(AnimationBinding binding,AnimationFollowDefinition definition);
    IAnimationHandle RegisterOscillation(AnimationBinding binding,AnimationOscillationDefinition definition);
    void ValidateOscillationKick(IAnimationHandle handle,AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime clockTime);
    void KickOscillation(IAnimationHandle handle,AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime clockTime);
    AnimationOscillationRead ReadOscillation(IAnimationHandle handle);
    IAnimationHandle RegisterImpulses(AnimationBinding binding,AnimationImpulseDefinition definition,AnimationValue from,AnimationValue peak);
    void ValidateImpulseAdmission(IAnimationHandle handle,AnimationOccurrenceId occurrence,AnimationStrength strength);
    void ReserveImpulses(IAnimationHandle handle, int additional);
    void EnqueueImpulse(IAnimationHandle handle,AnimationOccurrenceId occurrence,AnimationStrength strength,PhysicalTime occurredAt);
    AnimationImpulseRead ReadImpulses(IAnimationHandle handle);
    void CancelImpulses(IAnimationHandle handle);
    void ValidateFollowValue(IAnimationHandle handle,AnimationValue target,PhysicalTime clockTime);
    void FollowValue(IAnimationHandle handle,AnimationValue target,PhysicalTime clockTime);
    void Start(IAnimationHandle handle);
    void StartAt(IAnimationHandle handle, PhysicalTime clockTime);
    void StartAtOccurrence(IAnimationHandle handle, PhysicalTime occurredAt);
    void DriveTo(IAnimationHandle handle, AnimationEndpoint endpoint, PhysicalTime clockTime);
    void SetLoopRate(IAnimationHandle handle, AnimationPlaybackRate rate, PhysicalTime clockTime);
    void StopPlayback(IAnimationHandle handle, AnimationStop policy, PhysicalTime clockTime);
    void SetVisible(IAnimationHandle handle, bool visible);
    AnimationBinding Binding(IAnimationHandle handle);
    AnimationBinding ScheduledBindingAt(int index);
    AnimationRead Read(IAnimationHandle handle);
    void Remove(IAnimationHandle handle);
    AnimationFrameWork Advance(ulong frame, PhysicalTime presentationTime, PhysicalTime simulationTime);
    void Reset();
}
