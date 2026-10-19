using System;

namespace CuriousContraptions.Gpu;

/// <summary>Why the Animation worker held a channel control instead of applying it.</summary>
public enum AnimationDeferralCause : uint { Preparation = 1, Recalibrating = 2 }

/// <summary>What one Animation service turn does.</summary>
public enum AnimationServiceTurn { Wait, ApplyDeferred, Evaluate }

/// <summary>
/// The Animation worker's one deferred channel control (Story 9.4b). A control that arrives while an installation is
/// prepared, or while the clock mapping recalibrates and has no master time, is held unapplied and so unacknowledged:
/// the browser sends no other, so the capacity is one. A service turn applies it once the installation is committed and
/// master time is available again, and that turn ends there: applying it reads a newer native time, so evaluation
/// resumes on the next turn and never at a reversed time. Every hold is a counted, typed
/// CCGPU_ANIMATION_CONTROL_DEFERRED telemetry line.
/// </summary>
public sealed class AnimationControlDeferral
{
    private byte[]? _held;

    public bool IsHolding => _held is not null;
    public ulong Deferrals { get; private set; }

    public void Hold(byte[] control, AnimationDeferralCause cause)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (!Enum.IsDefined(cause)) throw new ArgumentOutOfRangeException(nameof(cause), cause, "Unknown animation deferral cause.");
        if (_held is not null) throw new InvalidOperationException("Deferred animation control capacity exceeded.");
        if (Deferrals == ulong.MaxValue) throw new InvalidOperationException("Animation deferral count exhausted.");
        _held = control;
        Deferrals++;
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("cause", (uint)cause);
            json.WriteString("causeName", Enum.GetName(cause));
            json.WriteString("count", Deferrals.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.WriteEndObject();
        }
        Console.WriteLine("CCGPU_ANIMATION_CONTROL_DEFERRED " + System.Text.Encoding.UTF8.GetString(bytes.ToArray()));
    }

    /// <summary>A turn waits without an installation, while one is prepared, or without master time; otherwise it applies
    /// the held control and returns, or evaluates when nothing is held.</summary>
    public AnimationServiceTurn Turn(bool installed, bool preparing, bool masterTimeAvailable) =>
        !installed || preparing || !masterTimeAvailable ? AnimationServiceTurn.Wait
        : _held is not null ? AnimationServiceTurn.ApplyDeferred : AnimationServiceTurn.Evaluate;

    public byte[]? Release()
    {
        var held = _held;
        _held = null;
        return held;
    }
}
