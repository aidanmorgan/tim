using System;

namespace CuriousContraptions;

/// <summary>Extensible declaration identity; never a display-name selector.</summary>
public sealed class TimerSlot;
public readonly record struct SceneTimerKey(MachinePart Owner,TimerSlot Slot);
public enum TimerElapsedSignal { None, Activation }
public readonly record struct SceneTimerDeclaration(SceneTimerKey Key,double DurationSeconds,
    TimerCompletionPolicy Completion,TimerBoundary Boundary,TimerElapsedSignal Signal)
{
    internal int DurationTicks(double tickSeconds)
    {
        if(!Enum.IsDefined(Signal)||Key.Owner is null||Key.Slot is null||!double.IsFinite(DurationSeconds)||DurationSeconds<=0||
            !double.IsFinite(tickSeconds)||tickSeconds<=0)
            throw new ArgumentException("Timer declaration requires an owner, slot and finite positive duration.");
        return checked((int)Math.Ceiling(DurationSeconds/tickSeconds));
    }
}
