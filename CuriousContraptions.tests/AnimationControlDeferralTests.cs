using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Verification gap V4 (Story 9.4b): the Animation worker's deferred channel control, natively. A control held
/// while an installation is prepared or while the clock mapping recalibrates stays unapplied (and so unacknowledged)
/// until a service turn finds the installation committed and master time available again; that turn applies it and
/// returns without evaluating. Every hold is one counted, typed CCGPU_ANIMATION_CONTROL_DEFERRED line.</summary>
public sealed class AnimationControlDeferralTests
{
    private static (string Output, T Result) Capture<T>(Func<T> action)
    {
        using var output = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var previous = Console.Out;
        try { Console.SetOut(output); var result = action(); return (output.ToString(), result); }
        finally { Console.SetOut(previous); }
    }

    [Fact]
    public void AControlHeldWhileRecalibratingWaitsUnappliedThenTheRequalifiedTurnAppliesItWithoutEvaluating()
    {
        var deferral = new AnimationControlDeferral();
        var control = new byte[] { 1, 2, 3 };
        Assert.Equal(AnimationServiceTurn.Evaluate, deferral.Turn(installed: true, preparing: false, masterTimeAvailable: true)); // control
        var (output, _) = Capture(() => { deferral.Hold(control, AnimationDeferralCause.Recalibrating); return 0; });
        Assert.True(deferral.IsHolding);
        // Held: every turn without master time waits, and neither applies nor evaluates.
        for (var turn = 0; turn < 3; turn++)
            Assert.Equal(AnimationServiceTurn.Wait, deferral.Turn(installed: true, preparing: false, masterTimeAvailable: false));
        Assert.True(deferral.IsHolding);
        // Requalified: this turn applies the held control and does not evaluate.
        Assert.Equal(AnimationServiceTurn.ApplyDeferred, deferral.Turn(installed: true, preparing: false, masterTimeAvailable: true));
        Assert.Same(control, deferral.Release());
        Assert.False(deferral.IsHolding);
        // Evaluation resumes on the next turn.
        Assert.Equal(AnimationServiceTurn.Evaluate, deferral.Turn(installed: true, preparing: false, masterTimeAvailable: true));
        var line = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("CCGPU_ANIMATION_CONTROL_DEFERRED ", line);
        using var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_ANIMATION_CONTROL_DEFERRED ".Length..]);
        Assert.Equal((uint)AnimationDeferralCause.Recalibrating, json.RootElement.GetProperty("cause").GetUInt32());
        Assert.Equal("Recalibrating", json.RootElement.GetProperty("causeName").GetString());
        Assert.Equal("1", json.RootElement.GetProperty("count").GetString());
        Assert.Equal(1UL, deferral.Deferrals);
    }

    [Fact]
    public void AControlHeldDuringPreparationWaitsForTheCommittedInstallation()
    {
        var deferral = new AnimationControlDeferral();
        Capture(() => { deferral.Hold([7], AnimationDeferralCause.Preparation); return 0; });
        Assert.Equal(AnimationServiceTurn.Wait, deferral.Turn(installed: true, preparing: true, masterTimeAvailable: true));
        Assert.Equal(AnimationServiceTurn.Wait, deferral.Turn(installed: false, preparing: false, masterTimeAvailable: true));
        Assert.Equal(AnimationServiceTurn.ApplyDeferred, deferral.Turn(installed: true, preparing: false, masterTimeAvailable: true));
    }

    [Fact]
    public void TheDeferralHoldsOneControlAndRejectsAnUnknownCause()
    {
        var deferral = new AnimationControlDeferral();
        Capture(() => { deferral.Hold([1], AnimationDeferralCause.Recalibrating); return 0; });
        Assert.Contains("capacity exceeded", Assert.Throws<InvalidOperationException>(() => deferral.Hold([2], AnimationDeferralCause.Preparation)).Message);
        Assert.Equal(new byte[] { 1 }, deferral.Release());
        Assert.Null(deferral.Release());
        Assert.Throws<ArgumentOutOfRangeException>(() => deferral.Hold([3], (AnimationDeferralCause)0));
        Assert.Throws<ArgumentNullException>(() => deferral.Hold(null!, AnimationDeferralCause.Recalibrating));
        Assert.Equal(1UL, deferral.Deferrals);
        Assert.Equal(AnimationServiceTurn.Wait, deferral.Turn(installed: false, preparing: false, masterTimeAvailable: false));
    }
}
