using CuriousContraptions.Abstractions;
using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public readonly record struct AnimationControlApplication(float Value, bool Started);

/// <summary>Session-owned control tracks. Physical-world retirement preserves autonomous UI tracks;
/// declared per-world presentation has its own independent owner.</summary>
public sealed class WorkshopAnimationTracks
{
    private readonly AnimationBatch Tracks = new();
    private Track[] Instances = new Track[16];
    private struct Track { public AnimationHandle? Handle; public WorkshopAnimationControl Control; }
    public int Capacity => Instances.Length;

    public void Validate(WorkshopAnimationControl control)
    {
        WorkshopAnimationWire.Validate(control);
        if (control.World.Value != 0 && control.Kind == AnimationControlKind.Reveal)
            throw new ArgumentException("Timed world channel requires a committed-world history.");
        var previous = default(Track);
        foreach (var item in Instances)
            if (item.Handle is not null && item.Control.Target == control.Target) { previous = item; break; }
        if (previous.Handle is not null && (control.Property != previous.Control.Property || control.World != previous.Control.World ||
            control.Generation < previous.Control.Generation ||
            (control.Kind == AnimationControlKind.Visibility && control.Generation != previous.Control.Generation)))
            throw new ArgumentException("Stale animation target generation.");
        if (control.Kind == AnimationControlKind.Visibility && previous.Handle is null)
            throw new ArgumentException("Visibility requires its registered target.");
    }

    public AnimationControlApplication Apply(WorkshopAnimationControl control, PhysicalTime masterTime)
    {
        Validate(control);
        var slot = -1; var vacant = -1; var occupied = 0;
        for (var i = 0; i < Instances.Length; i++)
        {
            if (Instances[i].Handle is null) vacant = i;
            else
            {
                occupied++;
                if (Instances[i].Control.Target == control.Target) slot = i;
            }
        }
        var required = occupied + (slot < 0 ? 1L : 0L);
        if (required > Array.MaxLength) throw new AnimationStorageException();
        if (slot < 0) slot = vacant < 0 ? Instances.Length : vacant;
        if (required > 3L * Instances.Length / 4 && Instances.Length < Array.MaxLength)
        {
            var capacity = (int)Math.Min(Array.MaxLength, Math.Max((4L * required + 2) / 3, 2L * Instances.Length));
            Track[] candidate;
            try { candidate = new Track[capacity]; Array.Copy(Instances, candidate, Instances.Length); }
            catch (OutOfMemoryException) { throw new AnimationStorageException(); }
            Tracks.EnsureCapacity((int)required);
            Instances = candidate;
        }
        var previous = Instances[slot];
        var started = false;
        if (control.Kind != AnimationControlKind.Visibility)
        {
            var from = control.From;
            var to = control.To;
            var start = control.Kind == AnimationControlKind.Reveal;
            var duration = control.Duration;
            var definition = new AnimationDefinition(WorkshopAnimationWire.Value(control.Property, from),
                WorkshopAnimationWire.Value(control.Property, to), new AnimationDurationSeconds(duration),
                control.Curve, AnimationRepeat.Once, AnimationClock.Presentation);
            if (previous.Handle is { } old) Tracks.Remove(old);
            var handle = Tracks.Register(new(control.Target, control.Property), definition);
            if (start)
            {
                Tracks.StartAt(handle, masterTime);
                started = true;
            }
            Tracks.SetVisible(handle, control.Visible);
            Instances[slot] = new() { Handle = handle, Control = control };
        }
        else
        {
            Tracks.SetVisible(previous.Handle!.Value, control.Visible);
            Instances[slot].Control = control;
        }
        return new(BitConverter.UInt32BitsToSingle(Tracks.Read(Instances[slot].Handle!.Value).Value.CanonicalBits), started);
    }

    public void Advance(ulong sequence, PhysicalTime presentationTime) => Tracks.Advance(sequence, presentationTime, default);

    public bool TryRead(int slot, out WorkshopAnimationControl control, out float value)
    {
        if ((uint)slot >= (uint)Instances.Length) throw new ArgumentOutOfRangeException(nameof(slot));
        var instance = Instances[slot];
        control = instance.Control;
        value = instance.Handle is { } handle ? BitConverter.UInt32BitsToSingle(Tracks.Read(handle).Value.CanonicalBits) : 0;
        return instance.Handle is not null;
    }

    public void RetireWorld(SimulationEpoch current)
    {
        if (current.Value == 0) throw new ArgumentException("World retirement requires a committed epoch.");
        for (var i = 0; i < Instances.Length; i++)
            if (Instances[i].Handle is { } retiring && Instances[i].Control.World.Value != 0 && Instances[i].Control.World != current)
            { Tracks.Remove(retiring); Instances[i] = default; }
    }
}
