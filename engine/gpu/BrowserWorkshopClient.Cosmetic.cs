using System;

namespace CuriousContraptions.Gpu;

/// <summary>The single main-thread cosmetic path: committed read → declared control → worker sample → one blend per part.</summary>
public sealed partial class BrowserWorkshopClient
{
    private bool TryCosmeticDeclaration(GpuBodyId owner, out CosmeticCurveDeclaration declaration)
    {
        declaration = default;
        if (_readConstruction is not { } construction) return false;
        foreach (var instance in construction.Instances)
        {
            if (instance.Id != owner) continue;
            declaration = instance.Cosmetic; declaration.Validate();
            return declaration.IsDeclared;
        }
        return false;
    }
    private bool TryCosmeticDeclaration(GpuBodyId owner, AnimationFeedbackSource source, out CosmeticCurveDeclaration declaration) =>
        TryCosmeticDeclaration(owner, out declaration) && declaration.Source == source;
    /// <summary>One-shot occurrence pumps precede the per-tick timer pump so a counting Delay cannot starve them of the lease.</summary>
    private void PumpCosmetics() { PumpActivations(); PumpContactFeedback(); PumpTimers(); }
    public bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed(); PumpGoal(); PumpCosmetics();
        sample = default;
        if (!TryCosmeticDeclaration(owner, out var declaration) || !AdmitPresentationFrame(frame)) return false;
        // Activation nodes and timers are keyed by their owning body identity in the compiled network.
        return declaration.Source switch
        {
            AnimationFeedbackSource.Activation => TryActivationSample(physical, new(owner.Value), out sample),
            AnimationFeedbackSource.Timer => TryTimerSample(physical, new(owner.Value), out sample),
            AnimationFeedbackSource.ContactWork => TryContactSample(physical, owner, out sample),
            _ => throw new ArgumentException("Unknown cosmetic feedback source.")
        };
    }
}
